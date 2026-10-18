using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KiloKilo.Helpers;
using KiloKilo.Services;

namespace KiloKilo.ViewModels;

public partial class RegisterViewModel : BaseViewModel
{
    private readonly IAuthService _auth;

    public RegisterViewModel(IAuthService auth, IKiloKiloService data)
    {
        _auth = auth;
        Marches = new ObservableCollection<string>(data.Marches.Select(m => m.Nom));
    }

    public ObservableCollection<string> Marches { get; }

    [ObservableProperty] private string nomComplet = string.Empty;
    [ObservableProperty] private string telephone = string.Empty;
    [ObservableProperty] private string? marcheReference;
    [ObservableProperty] private string budget = string.Empty;
    [ObservableProperty] private string pin = string.Empty;
    [ObservableProperty] private bool accepteConditions;
    [ObservableProperty] private string erreur = string.Empty;

    public bool HasErreur => !string.IsNullOrEmpty(Erreur);
    partial void OnErreurChanged(string value) => OnPropertyChanged(nameof(HasErreur));

    [RelayCommand]
    private async Task CreerAsync()
    {
        Erreur = string.Empty;
        if (string.IsNullOrWhiteSpace(NomComplet)) { Erreur = "Le nom complet est requis."; return; }
        if (string.IsNullOrWhiteSpace(Telephone)) { Erreur = "Le téléphone / Mobile Money est requis."; return; }
        if (string.IsNullOrWhiteSpace(MarcheReference)) { Erreur = "Choisissez votre marché de référence."; return; }
        if (Pin.Length != 4 || !Pin.All(char.IsDigit)) { Erreur = "Le code secret comporte 4 chiffres."; return; }
        if (!AccepteConditions) { Erreur = "Acceptez les conditions d'utilisation pour continuer."; return; }

        decimal? b = Fcfa.TryParse(Budget, out var v) ? v : null;

        IsBusy = true;
        try
        {
            await _auth.RegisterAsync(NomComplet.Trim(), Telephone.Trim(), MarcheReference!, b, Pin);
            await Shell.Current.GoToAsync(Routes.Accueil);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private Task SeConnecterAsync() => Shell.Current.GoToAsync(Routes.Login);
}
