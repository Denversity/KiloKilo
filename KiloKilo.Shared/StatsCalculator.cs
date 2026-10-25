namespace KiloKilo.Shared;

/// <summary>Calcule les statistiques de dépenses. Utilisé par l'API et, hors ligne, par l'application.</summary>
public static class StatsCalculator
{
    public static StatsDto Calculer(IEnumerable<CourseDto> courses, string? periode, DateOnly auj)
    {
        var cs = courses.Where(c => !c.Supprime).ToList();
        periode = periode is "Semaine" or "Année" ? periode : "Mois";

        DateOnly debut, fin, debutPrec, finPrec;
        string texte, rythme;
        switch (periode)
        {
            case "Semaine":
                debut = auj.AddDays(-(((int)auj.DayOfWeek + 6) % 7));
                fin = debut.AddDays(6);
                debutPrec = debut.AddDays(-7); finPrec = debut.AddDays(-1);
                texte = "semaine précédente"; rythme = "7 jours";
                break;
            case "Année":
                debut = new DateOnly(auj.Year, auj.Month, 1).AddMonths(-11);
                fin = auj;
                debutPrec = debut.AddMonths(-12); finPrec = debut.AddDays(-1);
                texte = "12 mois précédents"; rythme = "6 mois";
                break;
            default:
                debut = new DateOnly(auj.Year, auj.Month, 1);
                fin = debut.AddMonths(1).AddDays(-1);
                debutPrec = debut.AddMonths(-1); finPrec = debut.AddDays(-1);
                texte = "mois précédent"; rythme = "4 semaines";
                break;
        }

        var courant = cs.Where(c => c.Date >= debut && c.Date <= fin).ToList();
        var prec = cs.Where(c => c.Date >= debutPrec && c.Date <= finPrec).ToList();
        var depense = courant.Sum(c => c.Total);
        var precedent = prec.Sum(c => c.Total);
        var panier = courant.Count == 0 ? 0 : Math.Round(depense / courant.Count, 0);
        var panierPrec = prec.Count == 0 ? 0 : precedent / prec.Count;

        var stats = new StatsDto
        {
            Periode = periode,
            Depense = depense,
            Precedent = precedent,
            PanierMoyen = panier,
            VariationPanierPct = panierPrec == 0 ? 0 : Math.Round((double)((panier - panierPrec) / panierPrec * 100m), 0),
            LibelleComparaison = $"vs {texte} ({Texte.Fcfa(precedent)})",
            TitreRythme = rythme,
            Barres = Barres(cs, periode, auj, debut, fin),
            Apercu = Apercu(cs)
        };

        // Répartition par famille de vivres
        stats.Categories = courant
            .SelectMany(c => c.Articles)
            .GroupBy(a => Categories.De(a.Nom))
            .Select(g => (Nom: g.Key, Montant: g.Sum(a => a.Prix)))
            .OrderByDescending(x => x.Montant)
            .Select(x => new CategorieDto { Nom = x.Nom, Montant = x.Montant, Pourcentage = depense == 0 ? 0 : Math.Round((double)(x.Montant / depense * 100m), 0) })
            .ToList();

        // Repère des marchés
        stats.Marches = courant
            .GroupBy(c => c.Marche)
            .Select(g => (Nom: g.Key, Courses: g.Count(), Montant: g.Sum(c => c.Total)))
            .OrderByDescending(x => x.Montant)
            .Select(x => new MarcheStatDto { Nom = x.Nom, Courses = x.Courses, Montant = x.Montant, Pourcentage = depense == 0 ? 0 : Math.Round((double)(x.Montant / depense * 100m), 0) })
            .ToList();

        return stats;
    }

    private static List<BarreDto> Barres(List<CourseDto> cs, string periode, DateOnly auj, DateOnly debut, DateOnly fin)
    {
        var items = new List<(string Label, decimal Montant)>();
        switch (periode)
        {
            case "Semaine":
                var jours = new[] { "Lun", "Mar", "Mer", "Jeu", "Ven", "Sam", "Dim" };
                for (var i = 0; i < 7; i++)
                {
                    var d = debut.AddDays(i);
                    items.Add((jours[i], cs.Where(c => c.Date == d).Sum(c => c.Total)));
                }
                break;
            case "Année":
                var premier = new DateOnly(auj.Year, auj.Month, 1);
                for (var i = 5; i >= 0; i--)
                {
                    var m = premier.AddMonths(-i);
                    var label = Texte.Fr.DateTimeFormat.GetAbbreviatedMonthName(m.Month).TrimEnd('.');
                    items.Add((char.ToUpper(label[0]) + label[1..], cs.Where(c => c.Date.Year == m.Year && c.Date.Month == m.Month).Sum(c => c.Total)));
                }
                break;
            default:
                var bornes = new[] { (1, 7), (8, 14), (15, 21), (22, 31) };
                for (var i = 0; i < 4; i++)
                {
                    var (a, b) = bornes[i];
                    items.Add(($"Sem {i + 1}", cs.Where(c => c.Date >= debut && c.Date <= fin && c.Date.Day >= a && c.Date.Day <= b).Sum(c => c.Total)));
                }
                break;
        }
        var max = items.Max(i => i.Montant);
        return items.Select(i => new BarreDto { Label = i.Label, Montant = i.Montant, EstPic = max > 0 && i.Montant == max }).ToList();
    }

    private static string Apercu(List<CourseDto> cs)
    {
        var meilleur = ProduitCalculator.Calculer(cs)
            .Where(p => p.Points.Count >= 2)
            .Select(p => (Produit: p, Var: ProduitCalculator.Variation(p)))
            .Where(x => Math.Abs(x.Var) >= 1)
            .OrderByDescending(x => Math.Abs(x.Var))
            .FirstOrDefault();

        if (meilleur.Produit is null)
            return "Enregistrez plusieurs courses du même produit pour suivre l'évolution de ses prix.";

        var verbe = meilleur.Var < 0 ? "baissé" : "augmenté";
        return $"Le prix de « {meilleur.Produit.Nom.ToLowerInvariant()} » a {verbe} de {Math.Abs(Math.Round(meilleur.Var, 0))} % depuis votre dernier relevé.";
    }
}
