using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KiloKilo.Helpers;
using KiloKilo.Models;
using KiloKilo.Services;

namespace KiloKilo.ViewModels;

public partial class ProduitsViewModel : BaseViewModel
{
    private readonly IKiloKiloService _data;
    private IReadOnlyList<ProduitStat> _all = Array.Empty<ProduitStat>();

    public ProduitsViewModel(IKiloKiloService data)
    {
        _data = data;
        Filtres = new ObservableCollection<ChipItem> { new("Tous", true), new("En baisse"), new("En hausse") };
    }

    public ObservableCollection<ChipItem> Filtres { get; }
    public ObservableCollection<ProduitStat> Produits { get; } = new();

    [ObservableProperty] private string recherche = string.Empty;
    [ObservableProperty] private string resume = string.Empty;
    [ObservableProperty] private bool aucunResultat;

    partial void OnRechercheChanged(string value) => Appliquer();

    public override async Task OnAppearingAsync()
    {
        _all = await _data.GetProduitsAsync();
        var baisses = _all.Count(p => p.EnBaisse);
        Resume = $"{_all.Count} produit{(_all.Count > 1 ? "s" : "")} suivi{(_all.Count > 1 ? "s" : "")} • {baisses} en baisse";
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
        var filtre = Filtres.FirstOrDefault(f => f.IsSelected)?.Label ?? "Tous";
        IEnumerable<ProduitStat> q = _all;
        if (filtre == "En baisse") q = q.Where(p => p.EnBaisse);
        else if (filtre == "En hausse") q = q.Where(p => p.EnHausse);

        var t = Recherche?.Trim();
        if (!string.IsNullOrEmpty(t)) q = q.Where(p => p.Nom.Contains(t, StringComparison.OrdinalIgnoreCase));

        Produits.Clear();
        foreach (var p in q) Produits.Add(p);
        AucunResultat = Produits.Count == 0;
    }

    [RelayCommand]
    private Task OuvrirAsync(ProduitStat produit)
        => Shell.Current.GoToAsync($"{Routes.Produit}?nom={Uri.EscapeDataString(produit.Nom)}");

    [RelayCommand]
    private Task NouvelleCourseAsync() => Shell.Current.GoToAsync(Routes.NouvelleCourse);
}
