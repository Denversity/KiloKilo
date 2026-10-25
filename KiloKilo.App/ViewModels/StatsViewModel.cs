using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KiloKilo.Helpers;
using KiloKilo.Models;
using KiloKilo.Services;

namespace KiloKilo.ViewModels;

public partial class StatsViewModel : BaseViewModel
{
    private readonly IKiloKiloService _data;

    public StatsViewModel(IKiloKiloService data)
    {
        _data = data;
        Periodes = new ObservableCollection<ChipItem> { new("Semaine"), new("Mois", true), new("Année") };
    }

    public ObservableCollection<ChipItem> Periodes { get; }
    public ObservableCollection<BarItem> Barres { get; } = new();
    public ObservableCollection<CategorieStat> Categories { get; } = new();
    public ObservableCollection<MarcheStat> Marches { get; } = new();

    [ObservableProperty] private string depenseAffichee = string.Empty;
    [ObservableProperty] private string comparaison = string.Empty;
    [ObservableProperty] private string panierMoyenAffiche = string.Empty;
    [ObservableProperty] private string variationAffichee = string.Empty;
    [ObservableProperty] private string titreRythme = string.Empty;
    [ObservableProperty] private string nbVisitesAffiche = string.Empty;
    [ObservableProperty] private string apercu = string.Empty;

    public override Task OnAppearingAsync() => ChargerAsync();

    private async Task ChargerAsync()
    {
        var periode = Periodes.FirstOrDefault(p => p.IsSelected)?.Label ?? "Mois";
        var s = await _data.GetStatsAsync(periode);

        DepenseAffichee = Fcfa.Format(s.Depense);
        Comparaison = s.LibelleComparaison;
        PanierMoyenAffiche = Fcfa.Format(s.PanierMoyen);
        VariationAffichee = Fcfa.Percent(s.VariationPanierPct);
        TitreRythme = s.TitreRythme;
        NbVisitesAffiche = $"{s.NbVisites} visites";
        Apercu = s.Apercu;

        Remplir(Barres, s.Barres);
        Remplir(Categories, s.Categories);
        Remplir(Marches, s.Marches);
    }

    private static void Remplir<T>(ObservableCollection<T> cible, IEnumerable<T> source)
    {
        cible.Clear();
        foreach (var i in source) cible.Add(i);
    }

    [RelayCommand]
    private async Task ChoisirPeriodeAsync(ChipItem chip)
    {
        foreach (var p in Periodes) p.IsSelected = p == chip;
        await ChargerAsync();
    }

    [RelayCommand]
    private async Task ExporterAsync()
    {
        try { await ExportHelper.ExportCsvAsync(await _data.GetCoursesAsync()); }
        catch (Exception ex) { await AlertAsync("Export impossible", ex.Message); }
    }
}
