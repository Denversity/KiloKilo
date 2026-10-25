using KiloKilo.Api.Data;
using KiloKilo.Shared;

namespace KiloKilo.Api.Services;

public static class Mapping
{
    public static ProfilDto ToDto(this UserEntity u) => new()
    {
        Id = u.Id,
        NomComplet = u.NomComplet,
        Telephone = u.Identifiant,
        Email = u.Email,
        Localisation = u.Localisation,
        BudgetMensuel = u.BudgetMensuel,
        SeuilAlertePct = u.SeuilAlertePct,
        MembreDepuis = u.CreeLe
    };

    public static MarcheDto ToDto(this MarcheEntity m) => new()
    {
        Id = m.Id, Nom = m.Nom, Description = m.Description, EstPrincipal = m.EstPrincipal
    };

    public static CourseDto ToDto(this CourseEntity c) => new()
    {
        Id = c.Id,
        Marche = c.Marche,
        Date = c.Date,
        ModePaiement = c.ModePaiement,
        Economie = c.Economie,
        NoteEconomie = c.NoteEconomie,
        UpdatedAt = c.UpdatedAt,
        Supprime = c.Supprime,
        Articles = c.Articles.OrderBy(a => a.Id).Select(a => new ArticleDto
        {
            Nom = a.Nom, Quantite = a.Quantite, Prix = a.Prix, PrixUnitaire = a.PrixUnitaire, Tendance = a.Tendance
        }).ToList()
    };

    /// <summary>Convertit un DTO en articles d'entité (les repères de prix sont ajoutés ensuite par Enrichissement).</summary>
    public static List<ArticleEntity> ToEntities(this IEnumerable<ArticleDto> articles) => articles.Select(a => new ArticleEntity
    {
        Nom = a.Nom.Trim(),
        NomNormalise = Texte.NormaliserNom(a.Nom),
        Quantite = (a.Quantite ?? string.Empty).Trim(),
        Prix = a.Prix
    }).ToList();
}

public static class Identifiants
{
    /// <summary>« +237 690 12 34 56 » → « +237690123456 » ; un e-mail est mis en minuscules.</summary>
    public static string Normaliser(string? valeur)
    {
        var v = (valeur ?? string.Empty).Trim();
        if (v.Contains('@')) return v.ToLowerInvariant();
        return new string(v.Where(c => char.IsDigit(c) || c == '+').ToArray());
    }
}

public static class Reponses
{
    public static IResult Erreur(int status, string detail, string? titre = null)
        => Results.Problem(detail: detail, statusCode: status, title: titre ?? "Requête refusée");
}
