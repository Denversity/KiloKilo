namespace KiloKilo.Shared;

/// <summary>Reconstitue l'historique de prix de chaque produit à partir des courses.</summary>
public static class ProduitCalculator
{
    public static List<ProduitDto> Calculer(IEnumerable<CourseDto> courses)
    {
        return courses
            .Where(c => !c.Supprime)
            .SelectMany(c => c.Articles.Select(a => (Course: c, Article: a)))
            .GroupBy(x => Texte.NormaliserNom(x.Article.Nom))
            .Where(g => g.Key.Length > 0)
            .Select(g =>
            {
                var tries = g.OrderBy(x => x.Course.Date).ToList();
                var dernier = tries[^1].Article;
                return new ProduitDto
                {
                    Nom = dernier.Nom,
                    Unite = Texte.AnalyserQuantite(dernier.Quantite).Unite,
                    Points = tries.Select(x => new PointPrixDto
                    {
                        Date = x.Course.Date,
                        Prix = Texte.PrixParUnite(x.Article),
                        Marche = x.Course.Marche,
                        Quantite = x.Article.Quantite
                    }).ToList()
                };
            })
            .OrderByDescending(p => p.Points[^1].Date).ThenBy(p => p.Nom)
            .ToList();
    }

    public static double Variation(ProduitDto p)
    {
        if (p.Points.Count < 2) return 0;
        var prec = p.Points[^2].Prix;
        return prec == 0 ? 0 : (double)((p.Points[^1].Prix - prec) / prec * 100m);
    }
}
