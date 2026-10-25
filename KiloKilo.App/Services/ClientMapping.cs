using KiloKilo.Models;
using KiloKilo.Shared;

namespace KiloKilo.Services;

/// <summary>Conversions entre les DTO de l'API et les modèles d'affichage de l'application.</summary>
public static class ClientMapping
{
    public static CourseDto ToDto(this Course c) => new()
    {
        Id = c.Id,
        Marche = c.Marche,
        Date = DateOnly.FromDateTime(c.Date),
        ModePaiement = c.ModePaiement,
        Economie = c.Economie,
        NoteEconomie = c.NoteEconomie,
        Articles = c.Articles.Select(a => new ArticleDto
        {
            Nom = a.Nom, Quantite = a.Quantite, Prix = a.Prix, PrixUnitaire = a.PrixUnitaire, Tendance = a.Tendance
        }).ToList()
    };

    public static Course ToModel(this CourseDto d) => new()
    {
        Id = d.Id,
        Marche = d.Marche,
        Date = d.Date.ToDateTime(TimeOnly.MinValue),
        ModePaiement = d.ModePaiement,
        Economie = d.Economie,
        NoteEconomie = d.NoteEconomie,
        Articles = d.Articles.Select(a => new ArticleCourse
        {
            Nom = a.Nom, Quantite = a.Quantite, Prix = a.Prix, PrixUnitaire = a.PrixUnitaire, Tendance = a.Tendance
        }).ToList()
    };

    public static Marche ToModel(this MarcheDto m) => new()
    {
        Id = m.Id, Nom = m.Nom, Description = m.Description, EstPrincipal = m.EstPrincipal
    };

    public static MarcheDto ToDto(this Marche m) => new()
    {
        Id = m.Id, Nom = m.Nom, Description = m.Description, EstPrincipal = m.EstPrincipal
    };

    public static UserProfile ToModel(this ProfilDto p) => new()
    {
        NomComplet = p.NomComplet,
        Telephone = p.Telephone,
        Email = p.Email ?? string.Empty,
        Localisation = p.Localisation,
        MembreDepuis = $"Membre depuis {p.MembreDepuis.ToString("MMMM yyyy", Texte.Fr)}",
        BudgetMensuel = p.BudgetMensuel,
        SeuilAlertePct = p.SeuilAlertePct
    };

    public static ProduitStat ToModel(this ProduitDto p) => new()
    {
        Nom = p.Nom,
        Unite = p.Unite,
        Photo = ArticleCourse.PhotoPour(p.Nom),
        Points = p.Points.Select(x => new PointPrix
        {
            Date = x.Date.ToDateTime(TimeOnly.MinValue), Prix = x.Prix, Marche = x.Marche, Quantite = x.Quantite
        }).ToList()
    };

    public static StatsData ToModel(this StatsDto s)
    {
        var max = s.Barres.Count == 0 ? 0 : s.Barres.Max(b => b.Montant);
        return new StatsData
        {
            Depense = s.Depense,
            Precedent = s.Precedent,
            VariationPanierPct = s.VariationPanierPct,
            PanierMoyen = s.PanierMoyen,
            LibelleComparaison = s.LibelleComparaison,
            TitreRythme = s.TitreRythme,
            Apercu = s.Apercu,
            Barres = s.Barres.Select(b => new BarItem
            {
                Label = b.Label, Montant = b.Montant, EstPic = b.EstPic,
                HauteurRequest = max == 0 ? 4 : Math.Max(4, (double)(b.Montant / max) * 110)
            }).ToList(),
            Categories = s.Categories.Select(c => new CategorieStat { Nom = c.Nom, Montant = c.Montant, Pourcentage = c.Pourcentage }).ToList(),
            Marches = s.Marches.Select(m => new MarcheStat { Nom = m.Nom, Courses = m.Courses, Montant = m.Montant, Pourcentage = m.Pourcentage }).ToList()
        };
    }
}
