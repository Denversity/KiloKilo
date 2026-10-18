using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KiloKilo.Helpers;
using KiloKilo.Services;

namespace KiloKilo.ViewModels;

public partial class ParametresViewModel : BaseViewModel
{
    private readonly IKiloKiloService _data;
    private bool _charge;

    public ParametresViewModel(IKiloKiloService data) => _data = data;

    [ObservableProperty] private string nomComplet = string.Empty;
    [ObservableProperty] private string telephone = string.Empty;
    [ObservableProperty] private string localisation = string.Empty;
    [ObservableProperty] private string budget = string.Empty;
    [ObservableProperty] private double seuilPct = 85;
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
        Localisation = p.Localisation;
        Budget = Fcfa.Number(p.BudgetMensuel);
        SeuilPct = p.SeuilAlertePct;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task EnregistrerAsync()
    {
        Erreur = string.Empty;
        if (string.IsNullOrWhiteSpace(NomComplet)) { Erreur = "Le nom complet est requis."; return; }
        if (string.IsNullOrWhiteSpace(Telephone)) { Erreur = "Le téléphone est requis."; return; }
        if (!Fcfa.TryParse(Budget, out var b) || b <= 0) { Erreur = "Indiquez un budget mensuel valide en FCFA."; return; }

        var p = _data.Profil;
        p.NomComplet = NomComplet.Trim();
        p.Telephone = Telephone.Trim();
        p.Localisation = string.IsNullOrWhiteSpace(Localisation) ? p.Localisation : Localisation.Trim();
        p.BudgetMensuel = b;
        p.SeuilAlertePct = (int)Math.Round(SeuilPct);

        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private Task RetourAsync() => Shell.Current.GoToAsync("..");
}
