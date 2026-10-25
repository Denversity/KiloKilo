using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KiloKilo.Helpers;
using KiloKilo.Models;
using KiloKilo.Services;

namespace KiloKilo.ViewModels;

public partial class HistoriqueViewModel : BaseViewModel
{
    private readonly IKiloKiloService _data;
    private IReadOnlyList<Course> _all = Array.Empty<Course>();

    public HistoriqueViewModel(IKiloKiloService data)
    {
        _data = data;
        var mois = DateTime.Today.ToString("MMMM yyyy", new CultureInfo("fr-FR"));
        Mois = char.ToUpper(mois[0]) + mois[1..];
        Filtres = new ObservableCollection<ChipItem>
        {
            new("Tout", true), new(Mois), new("Marché Central"), new("Mokolo")
        };
    }

    public string Mois { get; }
    public ObservableCollection<ChipItem> Filtres { get; }
    public ObservableCollection<Course> Courses { get; } = new();

    [ObservableProperty] private string recherche = string.Empty;
    [ObservableProperty] private string cumulAffiche = string.Empty;
    [ObservableProperty] private string nbSortiesAffiche = string.Empty;
    [ObservableProperty] private bool aucunResultat;

    public string Astuce => "Les prix des vivres frais au Marché Mokolo sont généralement 15 % plus bas les mardis et jeudis à l'aube.";

    partial void OnRechercheChanged(string value) => Appliquer();

    public override async Task OnAppearingAsync()
    {
        _all = await _data.GetCoursesAsync();
        var auj = DateTime.Today;
        var mois = _all.Where(c => c.Date.Year == auj.Year && c.Date.Month == auj.Month).ToList();
        CumulAffiche = Fcfa.Format(mois.Sum(c => c.Total));
        NbSortiesAffiche = $"Cumul mensuel • {mois.Count} sorties";
        Appliquer();
    }

    [RelayCommand]
    private void ChoisirFiltre(ChipItem chip)
    {
        foreach (var f in Filtres) f.IsSelected = f == chip;
        Appliquer();
    }

    private void Appliquer()
    {
        var filtre = Filtres.FirstOrDefault(f => f.IsSelected)?.Label ?? "Tout";
        IEnumerable<Course> q = _all;

        if (filtre == Mois) q = q.Where(c => c.MoisLabel.Equals(Mois, StringComparison.OrdinalIgnoreCase));
        else if (filtre != "Tout") q = q.Where(c => c.Marche.Contains(filtre.Replace("Marché ", ""), StringComparison.OrdinalIgnoreCase));

        var t = Recherche?.Trim();
        if (!string.IsNullOrEmpty(t))
            q = q.Where(c => c.Marche.Contains(t, StringComparison.OrdinalIgnoreCase)
                          || c.Articles.Any(a => a.Nom.Contains(t, StringComparison.OrdinalIgnoreCase)));

        Courses.Clear();
        foreach (var c in q) Courses.Add(c);
        AucunResultat = Courses.Count == 0;
    }

    [RelayCommand]
    private Task OuvrirAsync(Course course)
        => Shell.Current.GoToAsync($"{Routes.DetailCourse}?id={course.Id}");

    [RelayCommand]
    private Task NouvelleCourseAsync() => Shell.Current.GoToAsync(Routes.NouvelleCourse);
}
