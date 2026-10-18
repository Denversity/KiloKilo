using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KiloKilo.Helpers;
using KiloKilo.Services;

namespace KiloKilo.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _auth;

    public LoginViewModel(IAuthService auth) => _auth = auth;

    [ObservableProperty] private string identifiant = string.Empty;
    [ObservableProperty] private string pin = string.Empty;
    [ObservableProperty] private bool memoriser = true;
    [ObservableProperty] private string erreur = string.Empty;

    public bool HasErreur => !string.IsNullOrEmpty(Erreur);
    partial void OnErreurChanged(string value) => OnPropertyChanged(nameof(HasErreur));

    [RelayCommand]
    private async Task ConnexionAsync()
    {
        Erreur = string.Empty;
        if (string.IsNullOrWhiteSpace(Identifiant)) { Erreur = "Saisissez votre identifiant ou votre téléphone."; return; }
        if (Pin.Length != 4 || !Pin.All(char.IsDigit)) { Erreur = "Le code secret comporte 4 chiffres."; return; }

        IsBusy = true;
        try
        {
            if (await _auth.LoginAsync(Identifiant, Pin))
                await Shell.Current.GoToAsync(Routes.Accueil);
            else
                Erreur = "Identifiants incorrects.";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private Task EmpreinteAsync()
        => AlertAsync("Connexion par empreinte", "La biométrie sera branchée avec le backend (prochain incrément).");

    [RelayCommand]
    private async Task HorsLigneAsync()
    {
        await _auth.LoginAsync("hors-ligne", "0000");
        await Shell.Current.GoToAsync(Routes.Accueil);
    }

    [RelayCommand]
    private Task CodeOublieAsync()
        => Shell.Current.GoToAsync(Routes.CodeOublie);

    [RelayCommand]
    private Task CreerCompteAsync() => Shell.Current.GoToAsync(Routes.Register);
}
