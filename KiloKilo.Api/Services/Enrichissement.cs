using System.Globalization;
using KiloKilo.Api.Data;
using KiloKilo.Shared;
using Microsoft.EntityFrameworkCore;

namespace KiloKilo.Api.Services;

/// <summary>
/// Calculs côté serveur à l'enregistrement d'une course :
///  - prix ramené à l'unité et tendance par rapport au dernier relevé de l'utilisateur ;
///  - économie constatée par rapport à la moyenne des relevés de la communauté (60 derniers jours).
/// </summary>
public static class Enrichissement
{
    private const int MinReleves = 3;

    public static async Task AppliquerAsync(KiloDb db, Guid userId, CourseEntity course)
    {
        var depuis = course.Date.AddDays(-60);
        decimal economie = 0;
        var references = 0;

        foreach (var a in course.Articles)
        {
            var (nombre, unite) = Texte.AnalyserQuantite(a.Quantite);
            a.PrixParUnite = Math.Round(a.Prix / nombre, 0);
            a.PrixUnitaire = string.IsNullOrEmpty(unite)
                ? null
                : $"{a.PrixParUnite.ToString("N0", Texte.Fr).Replace('\u202F', '\u00A0')} F/{unite}";

            // Tendance : dernier prix payé par cet utilisateur pour ce produit
            var precedent = await db.Articles
                .Where(x => x.NomNormalise == a.NomNormalise && x.Course.UserId == userId && !x.Course.Supprime
                            && x.CourseId != course.Id && x.Course.Date <= course.Date)
                .OrderByDescending(x => x.Course.Date)
                .Select(x => (decimal?)x.PrixParUnite)
                .FirstOrDefaultAsync();

            if (precedent is > 0)
            {
                var pct = (double)((a.PrixParUnite - precedent.Value) / precedent.Value * 100m);
                a.Tendance = Math.Abs(pct) < 1
                    ? "stable vs dernier relevé"
                    : $"{(pct > 0 ? "+" : "-")}{Math.Abs(Math.Round(pct, 0)).ToString(CultureInfo.InvariantCulture)} % vs dernier relevé";
            }
            else a.Tendance = null;

            // Moyenne communautaire (autres utilisateurs)
            var autres = await db.Articles
                .Where(x => x.NomNormalise == a.NomNormalise && x.Course.UserId != userId && !x.Course.Supprime
                            && x.Course.Date >= depuis && x.Course.Date <= course.Date)
                .Select(x => x.PrixParUnite)
                .ToListAsync();

            if (autres.Count >= MinReleves)
            {
                var moyenne = autres.Average();
                if (a.PrixParUnite < moyenne)
                {
                    economie += (moyenne - a.PrixParUnite) * nombre;
                    references += autres.Count;
                }
            }
        }

        course.Economie = Math.Round(economie, 0);
        course.NoteEconomie = course.Economie > 0
            ? $"par rapport à la moyenne des relevés de la communauté ({references} relevés des 60 derniers jours)."
            : null;
    }
}
