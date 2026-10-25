using KiloKilo.Api.Data;
using KiloKilo.Api.Services;
using KiloKilo.Shared;
using Microsoft.EntityFrameworkCore;

namespace KiloKilo.Api.Endpoints;

public static class ProfilEndpoints
{
    public static void MapProfil(this WebApplication app)
    {
        var p = app.MapGroup("/api/profil").RequireAuthorization().WithTags("Profil");

        p.MapGet("/", async (System.Security.Claims.ClaimsPrincipal me, KiloDb db) =>
        {
            var user = await db.Users.FindAsync(me.UserId());
            return user is null ? Results.NotFound() : Results.Ok(user.ToDto());
        });

        p.MapPut("/", async (ProfilDto dto, System.Security.Claims.ClaimsPrincipal me, KiloDb db) =>
        {
            var user = await db.Users.FindAsync(me.UserId());
            if (user is null) return Results.NotFound();

            var nom = (dto.NomComplet ?? string.Empty).Trim();
            if (nom.Length < 2 || nom.Length > 120) return Reponses.Erreur(400, "Le nom complet est requis (2 à 120 caractères).");
            if (dto.BudgetMensuel is < 0 or > 1_000_000_000) return Reponses.Erreur(400, "Budget mensuel invalide.");
            if (dto.SeuilAlertePct is < 1 or > 100) return Reponses.Erreur(400, "Le seuil d'alerte doit être compris entre 1 et 100 %.");

            var ident = Identifiants.Normaliser(dto.Telephone);
            if (ident.Contains('@') || ident.Count(char.IsDigit) is < 8 or > 15) return Reponses.Erreur(400, "Numéro de téléphone invalide.");
            if (ident != user.Identifiant && await db.Users.AnyAsync(u => u.Identifiant == ident))
                return Reponses.Erreur(409, "Ce numéro est déjà utilisé par un autre compte.");

            var email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim().ToLowerInvariant();
            if (email is not null && (!email.Contains('@') || email.Length > 120)) return Reponses.Erreur(400, "Adresse e-mail invalide.");
            if (email is not null && email != user.Email && await db.Users.AnyAsync(u => u.Email == email))
                return Reponses.Erreur(409, "Cette adresse e-mail est déjà utilisée.");

            user.NomComplet = nom;
            user.Identifiant = ident;
            user.Email = email;
            user.Localisation = (dto.Localisation ?? string.Empty).Trim();
            user.BudgetMensuel = dto.BudgetMensuel;
            user.SeuilAlertePct = dto.SeuilAlertePct;
            await db.SaveChangesAsync();
            return Results.Ok(user.ToDto());
        });

        var m = app.MapGroup("/api/marches").RequireAuthorization().WithTags("Marchés");

        m.MapGet("/", async (System.Security.Claims.ClaimsPrincipal me, KiloDb db) =>
        {
            var uid = me.UserId();
            var liste = await db.Marches.Where(x => x.UserId == uid).OrderByDescending(x => x.EstPrincipal).ThenBy(x => x.Nom).ToListAsync();
            return Results.Ok(liste.Select(x => x.ToDto()));
        });

        m.MapPut("/{id:guid}", async (Guid id, MarcheDto dto, System.Security.Claims.ClaimsPrincipal me, KiloDb db) =>
        {
            var uid = me.UserId();
            var nom = (dto.Nom ?? string.Empty).Trim();
            if (nom.Length is 0 or > 80) return Reponses.Erreur(400, "Le nom du marché est requis (80 caractères maximum).");

            var e = await db.Marches.FindAsync(id);
            if (e is not null && e.UserId != uid) return Results.NotFound();
            if (e is null) { e = new MarcheEntity { Id = id, UserId = uid }; db.Marches.Add(e); }

            e.Nom = nom;
            e.Description = (dto.Description ?? string.Empty).Trim();
            e.EstPrincipal = dto.EstPrincipal;
            if (dto.EstPrincipal)
                foreach (var autre in db.Marches.Where(x => x.UserId == uid && x.Id != id && x.EstPrincipal)) autre.EstPrincipal = false;

            await db.SaveChangesAsync();
            return Results.Ok(e.ToDto());
        });

        m.MapDelete("/{id:guid}", async (Guid id, System.Security.Claims.ClaimsPrincipal me, KiloDb db) =>
        {
            var uid = me.UserId();
            var e = await db.Marches.FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);
            if (e is not null) { db.Marches.Remove(e); await db.SaveChangesAsync(); }
            return Results.NoContent();
        });
    }
}
