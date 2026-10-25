using KiloKilo.Api.Data;
using KiloKilo.Api.Services;
using KiloKilo.Shared;
using Microsoft.EntityFrameworkCore;

namespace KiloKilo.Api;

/// <summary>
/// Données de démonstration (environnement Development uniquement) :
///  - compte démo : +237690000000 / PIN 1234 ;
///  - un compte « communauté » dont les relevés alimentent la moyenne des prix.
/// </summary>
public static class Seed
{
    public static async Task ExecuterAsync(KiloDb db)
    {
        if (await db.Users.AnyAsync()) return;

        var auj = DateOnly.FromDateTime(DateTime.Today);
        var debutMois = new DateOnly(auj.Year, auj.Month, 1);

        var demo = new UserEntity
        {
            NomComplet = "Amina Nguemo", Identifiant = "+237690000000", PinHash = PinHasher.Hash("1234"),
            Localisation = "Yaoundé, Cameroun", BudgetMensuel = 150_000, SeuilAlertePct = 85,
            CreeLe = new DateOnly(2024, 8, 12)
        };
        var communaute = new UserEntity
        {
            NomComplet = "Communauté KiloKilo", Identifiant = "+237690000001", PinHash = PinHasher.Hash("0000"),
            Localisation = "Yaoundé, Cameroun", BudgetMensuel = 0, CreeLe = new DateOnly(2024, 8, 1)
        };
        db.Users.AddRange(demo, communaute);
        db.Marches.AddRange(
            new MarcheEntity { Id = Guid.NewGuid(), UserId = demo.Id, Nom = "Marché Central", Description = "Avenue Kennedy • Primeurs & Vivres frais", EstPrincipal = true },
            new MarcheEntity { Id = Guid.NewGuid(), UserId = demo.Id, Nom = "Marché Mokolo", Description = "Secteur Céréales & Gros" },
            new MarcheEntity { Id = Guid.NewGuid(), UserId = demo.Id, Nom = "Marché Mfoundi", Description = "Berge de Mfoundi • Poissons & Féculents" });
        await db.SaveChangesAsync();

        // Relevés de la communauté (prix un peu plus élevés : l'utilisateur démo « économise »)
        foreach (var (avant, marche, nom, qte, prix) in new (int, string, string, string, decimal)[]
        {
            (20, "Marché Mokolo", "Tomates rondes", "1 seau", 3_900), (14, "Marché Central", "Tomates rondes", "1 seau", 4_000),
            (9, "Marché Mokolo", "Tomates rondes", "1 seau", 3_800), (6, "Marché Central", "Tomates rondes", "1 seau", 4_100),
            (18, "Marché Central", "Riz parfumé", "Sac de 5kg", 4_000), (12, "Marché Mokolo", "Riz parfumé", "Sac de 5kg", 3_950),
            (7, "Marché Central", "Riz parfumé", "Sac de 5kg", 4_050),
            (16, "Marché Central", "Plantains mûrs", "1 régime moyen", 5_400), (10, "Marché Mfoundi", "Plantains mûrs", "1 régime moyen", 5_500),
            (5, "Marché Central", "Plantains mûrs", "1 régime moyen", 5_450),
        })
            await AjouterAsync(db, communaute.Id, marche, auj.AddDays(-avant), (nom, qte, prix));

        // Relevés des mois précédents de l'utilisateur démo (pour la courbe des prix)
        var anciens = new (int avant, string marche, string nom, string qte, decimal prix)[]
        {
            (55, "Marché Central", "Tomates rondes", "1 seau", 3_000), (55, "Marché Central", "Riz parfumé", "Sac de 5kg", 3_900), (55, "Marché Central", "Plantains mûrs", "1 régime moyen", 4_800),
            (41, "Marché Central", "Tomates rondes", "1 seau", 3_200), (41, "Marché Mokolo", "Bar frais", "3 kg", 3_300), (41, "Marché Mokolo", "Oignons blancs", "1 tas", 1_500),
            (30, "Marché Central", "Riz parfumé", "Sac de 5kg", 3_850), (30, "Marché Central", "Plantains mûrs", "1 régime moyen", 5_000), (30, "Marché Mfoundi", "Huile de palme", "2 L", 5_000),
            (27, "Marché Central", "Tomates rondes", "1 seau", 3_400),
            (18, "Marché Mokolo", "Bar frais", "3 kg", 3_400), (18, "Marché Mokolo", "Oignons blancs", "1 tas", 1_600), (18, "Marché Mokolo", "Piment", "1 tas", 1_200),
            (13, "Marché Central", "Tomates rondes", "1 seau", 3_300), (13, "Marché Central", "Plantains mûrs", "1 régime moyen", 5_300), (13, "Marché Mfoundi", "Huile de palme", "2 L", 5_200),
            (10, "Marché Central", "Riz parfumé", "Sac de 5kg", 3_800),
            (7, "Marché Mokolo", "Bar frais", "3 kg", 3_600), (7, "Marché Mokolo", "Oignons blancs", "1 tas", 1_700), (7, "Marché Mokolo", "Piment", "1 tas", 1_250),
        };
        foreach (var g in anciens.GroupBy(a => (a.avant, a.marche)).OrderByDescending(g => g.Key.avant))
            await AjouterAsync(db, demo.Id, g.Key.marche, debutMois.AddDays(-g.Key.avant), g.Select(a => (a.nom, a.qte, a.prix)).ToArray());

        // Mois courant : 12 sorties, 86 500 FCFA au total (comme les maquettes)
        DateOnly Jour(int avant) => new(auj.Year, auj.Month, Math.Max(1, auj.Day - avant));
        var mois = new (DateOnly date, string marche, (string, string, decimal)[] articles)[]
        {
            (Jour(15), "Marché Central", new[] { ("Viande de bœuf", "1 kg", 7_500m), ("Légumes verts", "3 bottes", 2_500m), ("Carottes", "1 kg", 1_800m), ("Poireaux", "2 tas", 1_500m), ("Pommes de terre", "5 kg", 5_000m) }),
            (Jour(12), "Marché Mokolo", new[] { ("Arachides", "1 seau", 2_700m) }),
            (Jour(11), "Marché Central", new[] { ("Œufs", "1 plaquette", 2_700m), ("Pain", "3 pièces", 2_700m) }),
            (Jour(10), "Marché Mfoundi", new[] { ("Sac de manioc", "1 sac", 9_700m), ("Huile de palme", "2 L", 5_500m) }),
            (Jour(9), "Marché Mokolo", new[] { ("Haricots", "2 kg", 3_300m) }),
            (Jour(8), "Marché Central", new[] { ("Avocats", "1 tas", 2_100m), ("Citrons", "1 tas", 2_000m) }),
            (Jour(6), "Marché Mfoundi", new[] { ("Poisson frais", "1 kg", 2_900m) }),
            (Jour(5), "Marché Mokolo", new[] { ("Bar frais", "3 kg", 3_500m), ("Oignons blancs", "1 tas", 1_800m), ("Piment", "1 tas", 1_300m), ("Gingembre", "1 tas", 2_000m) }),
            (Jour(4), "Marché Central", new[] { ("Ananas", "2 pièces", 1_600m), ("Papaye", "1 pièce", 4_000m) }),
            (Jour(3), "Marché Mokolo", new[] { ("Poisson fumé", "1 pièce", 2_300m), ("Piment", "1 tas", 1_500m) }),
            (Jour(2), "Marché Central", new[] { ("Oignons", "1 filet", 2_400m), ("Ail", "1 tas", 1_800m) }),
            (auj.Day > 1 ? auj.AddDays(-1) : auj, "Marché Central", new[] { ("Tomates rondes", "1 seau • environ 5kg", 3_500m), ("Riz parfumé", "Sac de 5kg", 3_750m), ("Plantains mûrs", "1 régime moyen", 5_150m) }),
        };
        foreach (var m in mois)
            await AjouterAsync(db, demo.Id, m.marche, m.date, m.articles);
    }

    private static async Task AjouterAsync(KiloDb db, Guid userId, string marche, DateOnly date, params (string nom, string qte, decimal prix)[] articles)
    {
        var course = new CourseEntity
        {
            Id = Guid.NewGuid(), UserId = userId, Marche = marche, Date = date, UpdatedAt = Clock.NowMs(),
            Articles = articles.Select(a => new ArticleDto { Nom = a.nom, Quantite = a.qte, Prix = a.prix }).ToEntities()
        };
        await Enrichissement.AppliquerAsync(db, userId, course);
        db.Courses.Add(course);
        await db.SaveChangesAsync();
    }
}
