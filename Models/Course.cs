using System.Globalization;
using KiloKilo.Helpers;

namespace KiloKilo.Models;

public class Course
{
    private static readonly CultureInfo Fr = new("fr-FR");

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Marche { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Today;
    public string ModePaiement { get; set; } = "Espèces";
    public List<ArticleCourse> Articles { get; set; } = new();

    /// <summary>Économie constatée par rapport à la moyenne d'un autre marché (FCFA).</summary>
    public decimal Economie { get; set; }
    public string? NoteEconomie { get; set; }

    public decimal Total => Articles.Sum(a => a.Prix);
    public string TotalAffiche => Fcfa.Format(Total);
    public string TotalNombre => Fcfa.Number(Total);

    public int NbArticles => Articles.Count;
    public string NbArticlesAffiche => $"{NbArticles} article{(NbArticles > 1 ? "s" : "")}";
    public string Resume => string.Join(", ", Articles.Select(a => a.Nom));

    public string DateAffichee
    {
        get
        {
            if (Date.Date == DateTime.Today) return "Aujourd'hui";
            if (Date.Date == DateTime.Today.AddDays(-1)) return "Hier";
            return Date.ToString("dd MMMM yyyy", Fr);
        }
    }

    public string DateLongue => Date.ToString("dd MMMM yyyy", Fr);
    public string MarcheEtDate => $"{Marche} • {DateLongue}";
    public string MoisLabel => Date.ToString("MMMM yyyy", Fr);

    public bool HasEconomie => Economie > 0;
    public string EconomieAffichee => $"-{Fcfa.Format(Economie)} économisés";
}
