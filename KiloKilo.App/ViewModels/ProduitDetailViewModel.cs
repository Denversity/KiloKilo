using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KiloKilo.Models;
using KiloKilo.Services;

namespace KiloKilo.ViewModels;

[QueryProperty(nameof(Nom), "nom")]
public partial class ProduitDetailViewModel : BaseViewModel
{
    private readonly IKiloKiloService _data;

    public ProduitDetailViewModel(IKiloKiloService data) => _data = data;

    [ObservableProperty] private string? nom;
    [ObservableProperty] private ProduitStat? produit;

    partial void OnNomChanged(string? value) => _ = ChargerAsync();

    public override Task OnAppearingAsync() => ChargerAsync();

    private async Task ChargerAsync()
    {
        if (string.IsNullOrWhiteSpace(Nom)) return;
        var cle = Uri.UnescapeDataString(Nom).Trim().ToLowerInvariant();
        var tous = await _data.GetProduitsAsync();
        Produit = tous.FirstOrDefault(p => p.Nom.Trim().ToLowerInvariant() == cle);
    }

    [RelayCommand]
    private Task RetourAsync() => Shell.Current.GoToAsync("..");
}
