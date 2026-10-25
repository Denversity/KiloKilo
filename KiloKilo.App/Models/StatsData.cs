using KiloKilo.Helpers;

namespace KiloKilo.Models;

public class BarItem
{
    public string Label { get; set; } = string.Empty;
    public decimal Montant { get; set; }
    public bool EstPic { get; set; }
    public double HauteurRequest { get; set; }
    public string MontantCourt => Fcfa.Short(Montant);
}

public class CategorieStat
{
    public string Nom { get; set; } = string.Empty;
    public decimal Montant { get; set; }
    public double Pourcentage { get; set; }
    public double Progress => Pourcentage / 100d;
    public string MontantAffiche => Fcfa.Format(Montant);
    public string PctAffiche => Fcfa.Percent(Pourcentage);
}

public class MarcheStat
{
    public string Nom { get; set; } = string.Empty;
    public int Courses { get; set; }
    public decimal Montant { get; set; }
    public double Pourcentage { get; set; }
    public string CoursesAffiche => $"{Courses} course{(Courses > 1 ? "s" : "")} effectuée{(Courses > 1 ? "s" : "")}";
    public string MontantAffiche => Fcfa.Format(Montant);
    public string PartAffichee => $"{Fcfa.Percent(Pourcentage)} du total";
}

public class StatsData
{
    public decimal Depense { get; set; }
    public decimal Precedent { get; set; }
    public double VariationPanierPct { get; set; }
    public decimal PanierMoyen { get; set; }
    public string LibelleComparaison { get; set; } = "vs mois précédent";
    public string TitreRythme { get; set; } = "4 semaines";
    public List<BarItem> Barres { get; set; } = new();
    public List<CategorieStat> Categories { get; set; } = new();
    public List<MarcheStat> Marches { get; set; } = new();
    public string Apercu { get; set; } = string.Empty;
    public int NbVisites => Marches.Sum(m => m.Courses);
}
