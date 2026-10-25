using System.Security.Claims;
using KiloKilo.Api.Data;
using KiloKilo.Api.Services;
using KiloKilo.Shared;
using Microsoft.EntityFrameworkCore;

namespace KiloKilo.Api.Endpoints;

public static class CourseEndpoints
{
    public static void MapCourses(this WebApplication app)
    {
        var g = app.MapGroup("/api/courses").RequireAuthorization().WithTags("Courses");

        // Liste complète (courses non supprimées)
        g.MapGet("/", async (ClaimsPrincipal me, KiloDb db) =>
        {
            var uid = me.UserId();
            var liste = await db.Courses.Include(c => c.Articles)
                .Where(c => c.UserId == uid && !c.Supprime)
                .OrderByDescending(c => c.Date).ToListAsync();
            return Results.Ok(liste.Select(c => c.ToDto()));
        });

        // Synchronisation incrémentale : tout ce qui a changé depuis « since » (suppressions incluses)
        g.MapGet("/sync", async (long? since, ClaimsPrincipal me, KiloDb db) =>
        {
            var uid = me.UserId();
            var maintenant = Clock.NowMs();
            var q = db.Courses.Include(c => c.Articles).Where(c => c.UserId == uid);
            q = since is > 0 ? q.Where(c => c.UpdatedAt > since) : q.Where(c => !c.Supprime);
            var liste = await q.ToListAsync();
            return Results.Ok(new SyncCoursesResponse { ServerTime = maintenant, Courses = liste.Select(c => c.ToDto()).ToList() });
        });

        g.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal me, KiloDb db) =>
        {
            var uid = me.UserId();
            var c = await db.Courses.Include(x => x.Articles).FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid && !x.Supprime);
            return c is null ? Results.NotFound() : Results.Ok(c.ToDto());
        });

        // Création ou mise à jour (idempotent : l'identifiant est généré par le client, utile hors ligne)
        g.MapPut("/{id:guid}", async (Guid id, CourseDto dto, ClaimsPrincipal me, KiloDb db) =>
        {
            var uid = me.UserId();
            if (dto.Id != Guid.Empty && dto.Id != id) return Reponses.Erreur(400, "L'identifiant de la course ne correspond pas à l'URL.");
            var erreur = Valider(dto);
            if (erreur is not null) return Reponses.Erreur(400, erreur);

            var entity = await db.Courses.Include(c => c.Articles).FirstOrDefaultAsync(c => c.Id == id);
            if (entity is not null && entity.UserId != uid) return Results.NotFound();

            if (entity is null)
            {
                entity = new CourseEntity { Id = id, UserId = uid };
                db.Courses.Add(entity);
            }
            else
            {
                db.Articles.RemoveRange(entity.Articles);
                entity.Articles.Clear();
            }

            entity.Marche = dto.Marche.Trim();
            entity.Date = dto.Date;
            entity.ModePaiement = string.IsNullOrWhiteSpace(dto.ModePaiement) ? "Espèces" : dto.ModePaiement.Trim();
            entity.Supprime = false;
            entity.UpdatedAt = Clock.NowMs();
            foreach (var a in dto.Articles.ToEntities()) entity.Articles.Add(a);

            await Enrichissement.AppliquerAsync(db, uid, entity);
            await db.SaveChangesAsync();
            return Results.Ok(entity.ToDto());
        });

        // Suppression logique (conservée pour que les autres appareils la reçoivent). Idempotente.
        g.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal me, KiloDb db) =>
        {
            var uid = me.UserId();
            var c = await db.Courses.FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);
            if (c is not null && !c.Supprime)
            {
                c.Supprime = true;
                c.UpdatedAt = Clock.NowMs();
                await db.SaveChangesAsync();
            }
            return Results.NoContent();
        });

        // Statistiques et prix par produit (calculés avec la même logique que l'application hors ligne)
        var s = app.MapGroup("/api").RequireAuthorization().WithTags("Statistiques");

        s.MapGet("/stats", async (string? periode, ClaimsPrincipal me, KiloDb db) =>
        {
            var courses = await ChargerAsync(db, me.UserId());
            return Results.Ok(StatsCalculator.Calculer(courses, periode, DateOnly.FromDateTime(DateTime.UtcNow)));
        });

        s.MapGet("/produits", async (ClaimsPrincipal me, KiloDb db) =>
        {
            var courses = await ChargerAsync(db, me.UserId());
            return Results.Ok(ProduitCalculator.Calculer(courses));
        });
    }

    private static async Task<List<CourseDto>> ChargerAsync(KiloDb db, Guid uid)
    {
        var liste = await db.Courses.Include(c => c.Articles).Where(c => c.UserId == uid && !c.Supprime).ToListAsync();
        return liste.Select(c => c.ToDto()).ToList();
    }

    private static string? Valider(CourseDto d)
    {
        if (string.IsNullOrWhiteSpace(d.Marche) || d.Marche.Trim().Length > 80) return "Le marché est requis (80 caractères maximum).";
        if (d.Date.Year < 2000 || d.Date > DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2))) return "Date de course invalide.";
        if (d.Articles is null || d.Articles.Count == 0) return "Une course doit contenir au moins un article.";
        if (d.Articles.Count > 100) return "Une course ne peut pas contenir plus de 100 articles.";
        foreach (var a in d.Articles)
        {
            if (string.IsNullOrWhiteSpace(a.Nom) || a.Nom.Trim().Length > 80) return "Le nom de chaque article est requis (80 caractères maximum).";
            if (a.Prix <= 0 || a.Prix > 100_000_000) return $"Prix invalide pour « {a.Nom} ».";
            if ((a.Quantite ?? string.Empty).Length > 60) return $"Quantité trop longue pour « {a.Nom} ».";
        }
        return null;
    }
}
