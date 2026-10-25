using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KiloKilo.Helpers;
using KiloKilo.Services;

namespace KiloKilo.ViewModels;

public partial class ParametresViewModel : BaseViewModel
{
    private readonly IKiloKiloService _data;
    private readonly ApiSettings _settings;
    private readonly ApiClient _api;
    private bool _charge;

    public ParametresViewModel(IKiloKiloService data, ApiSettings settings, ApiClient api)
    {
        _data = data;
        _settings = settings;
        _api = api;
    }

    [ObservableProperty] private string nomComplet = string.Empty;
    [ObservableProperty] private string telephone = string.Empty;
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string localisation = string.Empty;
    [ObservableProperty] private string budget = string.Empty;
    [ObservableProperty] private double seuilPct = 85;
    [ObservableProperty] private string serveurUrl = string.Empty;
    [ObservableProperty] private string erreur = string.Empty;

    public bool HasErreur => !string.IsNullOrEmpty(Erreur);
    partial void OnErreurChanged(string value) => OnPropertyChanged(nameof(HasErreur));

    public string SeuilAffiche
    {
        get
        {
            var pct = (int)Math.Round(SeuilPct);
            return Fcfa.TryParse(Budget, out var b) && b > 0
                ? $"{pct} % du budget, soit {Fcfa.Format(Math.Round(b * pct / 100m))}"
                : $"{pct} % du budget";
        }
    }

    partial void OnSeuilPctChanged(double value) => OnPropertyChanged(nameof(SeuilAffiche));
    partial void OnBudgetChanged(string value) => OnPropertyChanged(nameof(SeuilAffiche));

    public override Task OnAppearingAsync()
    {
        if (_charge) return Task.CompletedTask;
        _charge = true;
        var p = _data.Profil;
        NomComplet = p.NomComplet;
        Telephone = p.Telephone;
        Email = p.Email;
        Localisation = p.Localisation;
        Budget = Fcfa.Number(p.BudgetMensuel);
        SeuilPct = p.SeuilAlertePct;
        ServeurUrl = _settings.BaseUrl;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task TesterServeurAsync()
    {
        var url = ApiSettings.Normaliser(ServeurUrl);
        var ok = await _api.TesterAsync(url);
        await AlertAsync(ok ? "Serveur joignable" : "Serveur injoignable", ok ? $"Connecté à {url}." : $"Aucune réponse de {url}.");
    }

    [RelayCommand]
    private async Task EnregistrerAsync()
    {
        Erreur = string.Empty;
        if (string.IsNullOrWhiteSpace(NomComplet)) { Erreur = "Le nom complet est requis."; return; }
        if (string.IsNullOrWhiteSpace(Telephone)) { Erreur = "Le téléphone est requis."; return; }
        if (!string.IsNullOrWhiteSpace(Email) && !Email.Contains('@')) { Erreur = "Adresse e-mail invalide."; return; }
        if (!Fcfa.TryParse(Budget, out var b) || b <= 0) { Erreur = "Indiquez un budget mensuel valide en FCFA."; return; }

        var p = _data.Profil;
        p.NomComplet = NomComplet.Trim();
        p.Telephone = Telephone.Trim();
        p.Email = Email.Trim();
        p.Localisation = Localisation.Trim();
        p.BudgetMensuel = b;
        p.SeuilAlertePct = (int)Math.Round(SeuilPct);
        await _data.SaveProfilAsync(p);

        _settings.BaseUrl = ServeurUrl;
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private Task RetourAsync() => Shell.Current.GoToAsync("..");
}
