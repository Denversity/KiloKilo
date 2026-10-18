using KiloKilo.Models;

namespace KiloKilo.Services;

public class MockKiloKiloService : IKiloKiloService
{
    private readonly List<Course> _courses;

    public MockKiloKiloService()
    {
        Profil = new UserProfile();
        Marches = new List<Marche>
        {
            new() { Nom = "Marché Central", Description = "Avenue Kennedy • Primeurs & Vivres frais", EstPrincipal = true },
            new() { Nom = "Marché Mokolo", Description = "Secteur Céréales & Gros" },
            new() { Nom = "Marché Mfoundi", Description = "Berge de Mfoundi • Poissons & Féculents" },
        };
        _courses = Seed();
    }

    public UserProfile Profil { get; }
    public IList<Marche> Marches { get; }

    public Task<IReadOnlyList<Course>> GetCoursesAsync()
        => Task.FromResult<IReadOnlyList<Course>>(_courses.OrderByDescending(c => c.Date).ToList());

    public Task<Course?> GetCourseAsync(Guid id)
        => Task.FromResult(_courses.FirstOrDefault(c => c.Id == id));

    public Task AddCourseAsync(Course course)
    {
        _courses.Add(course);
        return Task.CompletedTask;
    }

    public Task UpdateCourseAsync(Course course)
    {
        var i = _courses.FindIndex(c => c.Id == course.Id);
        if (i >= 0) _courses[i] = course; else _courses.Add(course);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ProduitStat>> GetProduitsAsync()
    {
        var produits = _courses
            .SelectMany(c => c.Articles.Select(a => (Course: c, Article: a)))
            .GroupBy(x => x.Article.NomNormalise)
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .Select(g =>
            {
                var tries = g.OrderBy(x => x.Course.Date).ToList();
                var dernier = tries[^1].Article;
                return new ProduitStat
                {
                    Nom = dernier.Nom,
                    Photo = dernier.Photo,
                    Unite = dernier.UnitePrix,
                    Points = tries.Select(x => new PointPrix
                    {
                        Date = x.Course.Date,
                        Prix = x.Article.PrixParUnite,
                        Marche = x.Course.Marche,
                        Quantite = x.Article.Quantite
                    }).ToList()
                };
            })
            .OrderByDescending(p => p.Points[^1].Date).ThenBy(p => p.Nom)
            .ToList();
        return Task.FromResult<IReadOnlyList<ProduitStat>>(produits);
    }

    public Task DeleteCourseAsync(Guid id)
    {
        _courses.RemoveAll(c => c.Id == id);
        return Task.CompletedTask;
    }

    public Task<StatsData> GetStatsAsync(string periode)
    {
        var data = periode switch
        {
            "Semaine" => Build(21_000m, 24_000m, "vs semaine précédente", "7 jours",
                new[] { ("Lun", 0m), ("Mar", 6_500m), ("Mer", 0m), ("Jeu", 5_200m), ("Ven", 0m), ("Sam", 9_300m), ("Dim", 0m) }),
            "Année" => Build(912_000m, 985_000m, "vs année précédente", "6 mois",
                new[] { ("Mai", 142_000m), ("Juin", 151_000m), ("Juil", 148_000m), ("Août", 159_000m), ("Sept", 94_000m), ("Oct", 86_500m) }),
            _ => Build(86_500m, 94_000m, "vs mois précédent (94 000 FCFA)", "4 semaines",
                new[] { ("Sem 1", 18_000m), ("Sem 2", 24_000m), ("Sem 3", 21_000m), ("Sem 4", 23_000m) }),
        };
        return Task.FromResult(data);
    }

    private StatsData Build(decimal depense, decimal precedent, string comparaison, string rythme, (string, decimal)[] bars)
    {
        var max = bars.Max(b => b.Item2);
        return new StatsData
        {
            Depense = depense,
            Precedent = precedent,
            LibelleComparaison = comparaison,
            TitreRythme = rythme,
            PanierMoyen = 7_200m,
            VariationPanierPct = -8,
            Barres = bars.Select(b => new BarItem
            {
                Label = b.Item1,
                Montant = b.Item2,
                EstPic = b.Item2 == max && max > 0,
                HauteurRequest = max == 0 ? 4 : Math.Max(4, (double)(b.Item2 / max) * 110)
            }).ToList(),
            Categories = new()
            {
                new() { Nom = "Légumes & Condiments", Montant = Math.Round(depense * 0.44m, 0), Pourcentage = 44 },
                new() { Nom = "Féculents & Céréales", Montant = Math.Round(depense * 0.28m, 0), Pourcentage = 28 },
                new() { Nom = "Viandes & Poissons", Montant = Math.Round(depense * 0.19m, 0), Pourcentage = 19 },
                new() { Nom = "Huiles & Épicerie", Montant = Math.Round(depense * 0.09m, 0), Pourcentage = 9 },
            },
            Marches = new()
            {
                new() { Nom = "Marché Central", Courses = 6, Montant = Math.Round(depense * 0.56m, 0), Pourcentage = 56 },
                new() { Nom = "Marché Mokolo", Courses = 4, Montant = Math.Round(depense * 0.33m, 0), Pourcentage = 33 },
                new() { Nom = "Marché Mfoundi", Courses = 2, Montant = Math.Round(depense * 0.11m, 0), Pourcentage = 11 },
            },
            Apercu = "Le prix moyen du plantain a diminué de 12 % sur les étals du marché Mfoundi."
        };
    }

    // Données de démonstration : 12 sorties ce mois-ci (86 500 FCFA, comme les maquettes)
    // + des relevés des mois précédents pour alimenter l'évolution des prix.
    private static List<Course> Seed()
    {
        var auj = DateTime.Today;
        var debutMois = new DateTime(auj.Year, auj.Month, 1);

        static Course Make(string marche, DateTime date, params (string nom, string qte, decimal prix)[] a) => new()
        {
            Marche = marche,
            Date = date,
            Articles = a.Select(x => new ArticleCourse { Nom = x.nom, Quantite = x.qte, Prix = x.prix }).ToList()
        };

        // Jours du mois courant : jamais dans le futur, jamais avant le 1er
        DateTime Jour(int joursAvant) => new(auj.Year, auj.Month, Math.Max(1, auj.Day - joursAvant));

        var hier = Make("Marché Central", auj.Day > 1 ? auj.AddDays(-1) : auj,
            ("Tomates rondes", "1 seau • environ 5kg", 3_500),
            ("Riz parfumé", "Sac de 5kg", 3_750),
            ("Plantains mûrs", "1 régime moyen", 5_150));
        hier.Articles[0].PrixUnitaire = "~700 F/kg"; hier.Articles[0].Tendance = "stable vs mois dernier";
        hier.Articles[1].PrixUnitaire = "750 F/kg"; hier.Articles[1].Tendance = "-6% baisse négociée";
        hier.Articles[2].Tendance = "Bonne affaire";
        hier.Economie = 850;
        hier.NoteEconomie = "par rapport à la moyenne du marché Mokolo. Calculé sur la base de 42 relevés citoyens consolidés ce mardi.";

        var courses = new List<Course>
        {
            hier,
            Make("Marché Mokolo", Jour(5), ("Bar frais", "3 kg", 3_500), ("Oignons blancs", "1 tas", 1_800), ("Piment", "1 tas", 1_300), ("Gingembre", "1 tas", 2_000)),
            Make("Marché Mfoundi", Jour(10), ("Sac de manioc", "1 sac", 9_700), ("Huile de palme", "2 L", 5_500)),
            Make("Marché Central", Jour(15), ("Viande de bœuf", "1 kg", 7_500), ("Légumes verts", "3 bottes", 2_500), ("Carottes", "1 kg", 1_800), ("Poireaux", "2 tas", 1_500), ("Pommes de terre", "5 kg", 5_000)),
            Make("Marché Central", Jour(2), ("Oignons", "1 filet", 2_400), ("Ail", "1 tas", 1_800)),
            Make("Marché Mokolo", Jour(3), ("Poisson fumé", "1 pièce", 2_300), ("Piment", "1 tas", 1_500)),
            Make("Marché Central", Jour(4), ("Ananas", "2 pièces", 1_600), ("Papaye", "1 pièce", 4_000)),
            Make("Marché Mfoundi", Jour(6), ("Poisson frais", "1 kg", 2_900)),
            Make("Marché Central", Jour(8), ("Avocats", "1 tas", 2_100), ("Citrons", "1 tas", 2_000)),
            Make("Marché Mokolo", Jour(9), ("Haricots", "2 kg", 3_300)),
            Make("Marché Central", Jour(11), ("Œufs", "1 plaquette", 2_700), ("Pain", "3 pièces", 2_700)),
            Make("Marché Mokolo", Jour(12), ("Arachides", "1 seau", 2_700)),
        };

        // Relevés des mois précédents (jours avant le 1er du mois courant)
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
        foreach (var g in anciens.GroupBy(a => (a.avant, a.marche)))
            courses.Add(Make(g.Key.marche, debutMois.AddDays(-g.Key.avant), g.Select(a => (a.nom, a.qte, a.prix)).ToArray()));

        return courses;
    }
}
