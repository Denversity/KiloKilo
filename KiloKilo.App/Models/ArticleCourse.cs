using KiloKilo.Helpers;
using KiloKilo.Shared;

namespace KiloKilo.Models;

public class ArticleCourse
{
    public string Nom { get; set; } = string.Empty;
    public string Quantite { get; set; } = string.Empty;
    public decimal Prix { get; set; }

    /// <summary>Repère de prix unitaire calculé par le serveur, ex. « 700 F/kg ».</summary>
    public string? PrixUnitaire { get; set; }

    /// <summary>Tendance calculée par le serveur, ex. « -6 % vs dernier relevé ».</summary>
    public string? Tendance { get; set; }

    public string PrixAffiche => Fcfa.Format(Prix);

    public string LigneQuantitePrix => string.IsNullOrWhiteSpace(Quantite) ? PrixAffiche : $"{Quantite} • {PrixAffiche}";

    /// <summary>Nombre d'unités achetées (« 3 kg » → 3).</summary>
    public decimal QuantiteNombre => Texte.AnalyserQuantite(Quantite).Nombre;

    /// <summary>Unité de la quantité (« kg », « seau »…), vide si non lisible.</summary>
    public string UnitePrix => Texte.AnalyserQuantite(Quantite).Unite;

    public decimal PrixParUnite => Math.Round(Prix / QuantiteNombre, 0);

    public string NomNormalise => Texte.NormaliserNom(Nom);

    public string Photo => PhotoPour(Nom);

    public static string PhotoPour(string nom)
    {
        var n = (nom ?? string.Empty).ToLowerInvariant();
        if (n.Contains("tomate")) return "tomates.jpg";
        if (n.Contains("plantain") || n.Contains("banane")) return "plantain.jpg";
        if (n.Contains("riz") || n.Contains("manioc") || n.Contains("farine")) return "riz_sac.jpg";
        return "marche_panier.jpg";
    }

    public bool HasRepere => !string.IsNullOrWhiteSpace(PrixUnitaire) || !string.IsNullOrWhiteSpace(Tendance);

    public string Repere => string.Join(" • ", new[] { PrixUnitaire, Tendance }.Where(s => !string.IsNullOrWhiteSpace(s)));

    public ArticleCourse Clone() => new() { Nom = Nom, Quantite = Quantite, Prix = Prix };
}
