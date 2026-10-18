using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KiloKilo.Helpers;
using KiloKilo.Models;
using KiloKilo.Services;

namespace KiloKilo.ViewModels;

public partial class HomeViewModel : BaseViewModel
{
    private readonly IKiloKiloService _data;

    public HomeViewModel(IKiloKiloService data) => _data = data;

    public ObservableCollection<Course> DernieresCourses { get; } = new();

    [ObservableProperty] private string prenom = string.Empty;
    [ObservableProperty] private string moisLabel = string.Empty;
    [ObservableProperty] private string enveloppeAffichee = string.Empty;
    [ObservableProperty] private double progression;
    [ObservableProperty] private string consommeAffiche = string.Empty;
    [ObservableProperty] private string panierMoyenAffiche = string.Empty;
    [ObservableProperty] private string nbCoursesAffiche = string.Empty;

    // Repères (statiques tant que le backend n'existe pas)
    public string IndiceVivres => "-4,2 %";
    public string IndiceVivresDetail => "Sur vos vivres frais cette semaine";
    public string RepereSaison => "Abondance avocats";
    public string RepereSaisonDetail => "Prix en baisse de 15% aux étals";

    public override async Task OnAppearingAsync()
    {
        var toutes = await _data.GetCoursesAsync();
        var auj = DateTime.Today;
        var courses = toutes.Where(c => c.Date.Year == auj.Year && c.Date.Month == auj.Month).ToList();
        var profil = _data.Profil;
        var depense = courses.Sum(c => c.Total);
        var budget = profil.BudgetMensuel;

        Prenom = profil.Prenom;
        MoisLabel = DateTime.Today.ToString("MMMM yyyy", new CultureInfo("fr-FR")).ToUpperInvariant();
        EnveloppeAffichee = $"{Fcfa.Number(depense)} / {Fcfa.Format(budget)}";
        Progression = budget == 0 ? 0 : Math.Min(1d, (double)(depense / budget));
        ConsommeAffiche = Fcfa.Percent(Progression * 100, 1);
        PanierMoyenAffiche = courses.Count == 0 ? "—" : Fcfa.Format(Math.Round(depense / courses.Count, 0));
        NbCoursesAffiche = $"{courses.Count} courses saisies";

        DernieresCourses.Clear();
        foreach (var c in courses.Take(3)) DernieresCourses.Add(c);
    }

    [RelayCommand]
    private Task EnregistrerPanierAsync() => Shell.Current.GoToAsync(Routes.NouvelleCourse);

    [RelayCommand]
    private Task VoirPrixAsync() => Shell.Current.GoToAsync(Routes.Prix);

    [RelayCommand]
    private Task ToutVoirAsync() => Shell.Current.GoToAsync(Routes.Historique);

    [RelayCommand]
    private Task OuvrirCourseAsync(Course course)
        => Shell.Current.GoToAsync($"{Routes.DetailCourse}?id={course.Id}");
}
