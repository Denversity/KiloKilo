using System.Globalization;
using System.Text.RegularExpressions;
using KiloKilo.Helpers;

namespace KiloKilo.Models;

public class ArticleCourse
{
    public string Nom { get; set; } = string.Empty;
    public string Quantite { get; set; } = string.Empty;
    public decimal Prix { get; set; }

    /// <summary>Repère de prix unitaire pondéré, ex. « ~700 F/kg ».</summary>
    public string? PrixUnitaire { get; set; }

    /// <summary>Tendance, ex. « stable vs mois dernier », « -6% baisse négociée ».</summary>
    public string? Tendance { get; set; }

    public string PrixAffiche => Fcfa.Format(Prix);

    private static readonly Regex QteRx = new(@"^\s*(\d+(?:[.,]\d+)?)\s*(\p{L}+)?", RegexOptions.Compiled);

    /// <summary>Nombre d'unités achetées (« 3 kg » → 3, « 1 seau » → 1, non lisible → 1).</summary>
    public decimal QuantiteNombre
    {
        get
        {
            var m = QteRx.Match(Quantite ?? string.Empty);
            return m.Success
                   && decimal.TryParse(m.Groups[1].Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var q)
                   && q > 0 ? q : 1;
        }
    }

    /// <summary>Unité de la quantité (« kg », « seau », « tas »…), vide si non lisible.</summary>
    public string UnitePrix => QteRx.Match(Quantite ?? string.Empty) is { Success: true } m ? m.Groups[2].Value : string.Empty;

    /// <summary>Prix ramené à l'unité achetée, pour comparer les relevés dans le temps.</summary>
    public decimal PrixParUnite => Math.Round(Prix / QuantiteNombre, 0);

    public string NomNormalise => (Nom ?? string.Empty).Trim().ToLowerInvariant();

    public string LigneQuantitePrix => string.IsNullOrWhiteSpace(Quantite) ? PrixAffiche : $"{Quantite} • {PrixAffiche}";

    public string Photo
    {
        get
        {
            var n = Nom.ToLowerInvariant();
            if (n.Contains("tomate")) return "tomates.jpg";
            if (n.Contains("plantain") || n.Contains("banane")) return "plantain.jpg";
            if (n.Contains("riz") || n.Contains("manioc") || n.Contains("farine")) return "riz_sac.jpg";
            return "marche_panier.jpg";
        }
    }

    public bool HasRepere => !string.IsNullOrWhiteSpace(PrixUnitaire) || !string.IsNullOrWhiteSpace(Tendance);

    public string Repere => string.Join(" ", new[] { PrixUnitaire, Tendance }.Where(s => !string.IsNullOrWhiteSpace(s)));

    public ArticleCourse Clone() => new()
    {
        Nom = Nom, Quantite = Quantite, Prix = Prix, PrixUnitaire = PrixUnitaire, Tendance = Tendance
    };
}
