using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KiloKilo.Helpers;
using KiloKilo.Services;

namespace KiloKilo.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private static bool _restaurationFaite;

    private readonly IAuthService _auth;
    private readonly ApiSettings _settings;
    private readonly ApiClient _api;

    public LoginViewModel(IAuthService auth, ApiSettings settings, ApiClient api)
    {
        _auth = auth;
        _settings = settings;
        _api = api;
        serveurUrl = settings.BaseUrl;
    }

    [ObservableProperty] private string identifiant = string.Empty;
    [ObservableProperty] private string pin = string.Empty;
    [ObservableProperty] private bool memoriser = true;
    [ObservableProperty] private string erreur = string.Empty;
    [ObservableProperty] private string serveurUrl;
    [ObservableProperty] private bool isServeurVisible;

    public bool HasErreur => !string.IsNullOrEmpty(Erreur);
    partial void OnErreurChanged(string value) => OnPropertyChanged(nameof(HasErreur));

    /// <summary>Au démarrage, rouvre directement la session mémorisée sur l'appareil.</summary>
    public override async Task OnAppearingAsync()
    {
        if (_restaurationFaite) return;
        _restaurationFaite = true;
        if (await _auth.TryRestoreSessionAsync())
            await Shell.Current.GoToAsync(Routes.Accueil);
    }

    [RelayCommand]
    private async Task ConnexionAsync()
    {
        Erreur = string.Empty;
        if (string.IsNullOrWhiteSpace(Identifiant)) { Erreur = "Saisissez votre identifiant ou votre téléphone."; return; }
        if (Pin.Length != 4 || !Pin.All(char.IsDigit)) { Erreur = "Le code secret comporte 4 chiffres."; return; }

        IsBusy = true;
        try
        {
            var r = await _auth.LoginAsync(Identifiant, Pin, Memoriser);
            if (r.Ok)
            {
                Pin = string.Empty;
                await Shell.Current.GoToAsync(Routes.Accueil);
            }
            else Erreur = r.Erreur ?? "Connexion impossible.";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private Task EmpreinteAsync()
        => AlertAsync("Connexion par empreinte", "La biométrie n'est pas disponible dans cette version. Utilisez votre code secret.");

    [RelayCommand]
    private async Task HorsLigneAsync()
    {
        Erreur = string.Empty;
        var r = await _auth.OfflineDirectAsync();
        if (r.Ok) await Shell.Current.GoToAsync(Routes.Accueil);
        else Erreur = r.Erreur ?? "Mode hors-ligne indisponible.";
    }

    [RelayCommand]
    private Task CodeOublieAsync() => Shell.Current.GoToAsync(Routes.CodeOublie);

    [RelayCommand]
    private Task CreerCompteAsync() => Shell.Current.GoToAsync(Routes.Register);

    // ---- Adresse du serveur ----
    [RelayCommand]
    private void BasculerServeur() => IsServeurVisible = !IsServeurVisible;

    [RelayCommand]
    private async Task EnregistrerServeurAsync()
    {
        _settings.BaseUrl = ServeurUrl;
        ServeurUrl = _settings.BaseUrl;
        var ok = await _api.TesterAsync(ServeurUrl);
        await AlertAsync(ok ? "Serveur joignable" : "Serveur injoignable",
            ok ? $"Connecté à {ServeurUrl}." : $"Adresse enregistrée ({ServeurUrl}), mais le serveur ne répond pas. Vérifiez qu'il est démarré et que le téléphone est sur le même réseau.");
    }
}
