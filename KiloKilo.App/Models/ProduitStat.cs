using System.Globalization;
using KiloKilo.Helpers;

namespace KiloKilo.Models;

public class PointPrix
{
    private static readonly CultureInfo Fr = new("fr-FR");

    public DateTime Date { get; set; }
    public decimal Prix { get; set; }
    public string Marche { get; set; } = string.Empty;
    public string Quantite { get; set; } = string.Empty;

    public string DateAffichee => Date.ToString("dd MMM yyyy", Fr);
    public string PrixAffiche => Fcfa.Format(Prix);
    public string MarcheEtQuantite => string.IsNullOrWhiteSpace(Quantite) ? Marche : $"{Marche} • {Quantite}";
}

/// <summary>Historique de prix d'un produit, reconstitué à partir des courses enregistrées.</summary>
public class ProduitStat
{
    private static readonly CultureInfo Fr = new("fr-FR");

    public string Nom { get; set; } = string.Empty;
    public string Photo { get; set; } = "marche_panier.jpg";
    public string Unite { get; set; } = string.Empty;

    /// <summary>Relevés triés du plus ancien au plus récent.</summary>
    public List<PointPrix> Points { get; set; } = new();
    public IEnumerable<PointPrix> PointsRecents => Points.OrderByDescending(p => p.Date);

    public PointPrix Dernier => Points[^1];
    public decimal PrixActuel => Dernier.Prix;
    public decimal Precedent => Points.Count > 1 ? Points[^2].Prix : Dernier.Prix;
    public decimal Min => Points.Min(p => p.Prix);
    public decimal Max => Points.Max(p => p.Prix);
    public decimal Moyenne => Math.Round(Points.Average(p => p.Prix), 0);

    public double VariationPct => Precedent == 0 ? 0 : (double)((PrixActuel - Precedent) / Precedent * 100m);
    public bool EnBaisse => VariationPct < -0.5;
    public bool EnHausse => VariationPct > 0.5;

    public string VariationAffichee
    {
        get
        {
            if (Points.Count < 2) return "Nouveau";
            if (!EnBaisse && !EnHausse) return "Stable";
            return (VariationPct > 0 ? "+" : "") + Fcfa.Percent(VariationPct, 1);
        }
    }

    public string PrixActuelAffiche => Fcfa.Format(PrixActuel);
    public string MinAffiche => Fcfa.Number(Min);
    public string MaxAffiche => Fcfa.Number(Max);
    public string MoyenneAffichee => Fcfa.Number(Moyenne);
    public string UniteAffichee => string.IsNullOrWhiteSpace(Unite) ? "par achat" : $"par {Unite}";
    public string NbReleves => $"{Points.Count} relevé{(Points.Count > 1 ? "s" : "")}";
    public string SousTitre => $"{NbReleves} • {UniteAffichee}";

    public string DateDernierReleve
    {
        get
        {
            var d = Dernier.Date.Date;
            if (d == DateTime.Today) return "aujourd'hui";
            if (d == DateTime.Today.AddDays(-1)) return "hier";
            return Dernier.Date.ToString("dd MMM yyyy", Fr);
        }
    }

    /// <summary>Utilisé comme repère pendant la saisie d'une course.</summary>
    public string DernierReleve => $"Dernier relevé : {PrixActuelAffiche} {UniteAffichee} ({Dernier.Marche}, {DateDernierReleve})";

    /// <summary>Marché où le prix moyen est le plus bas (si au moins deux marchés comparés).</summary>
    public string MeilleurMarche
    {
        get
        {
            var groupes = Points.GroupBy(p => p.Marche).Select(g => (Marche: g.Key, Moy: g.Average(p => p.Prix))).ToList();
            if (groupes.Count < 2) return string.Empty;
            var best = groupes.OrderBy(g => g.Moy).First();
            return $"{best.Marche} (en moyenne {Fcfa.Format(Math.Round(best.Moy, 0))} {UniteAffichee})";
        }
    }
    public bool HasMeilleurMarche => !string.IsNullOrEmpty(MeilleurMarche);
}
