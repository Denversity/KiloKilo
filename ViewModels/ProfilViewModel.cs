using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KiloKilo.Helpers;
using KiloKilo.Models;
using KiloKilo.Services;

namespace KiloKilo.ViewModels;

public partial class ProfilViewModel : BaseViewModel
{
    private readonly IKiloKiloService _data;
    private readonly IAuthService _auth;

    public ProfilViewModel(IKiloKiloService data, IAuthService auth)
    {
        _data = data;
        _auth = auth;
        Unites = new ObservableCollection<string> { "Kilogramme (kg)", "Seau 5L / 15L", "Tas", "Unité", "Régime plantain", "Pièce", "Sac 50kg" };
    }

    public ObservableCollection<Marche> Marches { get; } = new();
    public ObservableCollection<string> Unites { get; }

    [ObservableProperty] private string nomComplet = string.Empty;
    [ObservableProperty] private string localisation = string.Empty;
    [ObservableProperty] private string membreDepuis = string.Empty;
    [ObservableProperty] private string economiesAffichees = string.Empty;
    [ObservableProperty] private string plafondAffiche = string.Empty;
    [ObservableProperty] private string seuilAffiche = string.Empty;
    [ObservableProperty] private string seuilDetail = string.Empty;
    [ObservableProperty] private string engageAffiche = string.Empty;
    [ObservableProperty] private double engageProgress;

    [ObservableProperty] private bool alerteDepassement = Preferences.Default.Get("alerte_depassement", true);
    [ObservableProperty] private bool notifArrivages = Preferences.Default.Get("notif_arrivages", true);
    [ObservableProperty] private bool modeSombre = Preferences.Default.Get("mode_sombre", false);

    public string Version => "KiloKilo v1.0.0 • Logiciel sobre & équitable pour marchés vivriers";

    partial void OnAlerteDepassementChanged(bool value) => Preferences.Default.Set("alerte_depassement", value);
    partial void OnNotifArrivagesChanged(bool value) => Preferences.Default.Set("notif_arrivages", value);

    partial void OnModeSombreChanged(bool value)
    {
        Preferences.Default.Set("mode_sombre", value);
        if (Application.Current is not null)
            Application.Current.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
    }

    public override async Task OnAppearingAsync()
    {
        var p = _data.Profil;
        var courses = await _data.GetCoursesAsync();
        var auj = DateTime.Today;
        var depense = courses.Where(c => c.Date.Year == auj.Year && c.Date.Month == auj.Month).Sum(c => c.Total);

        NomComplet = p.NomComplet;
        Localisation = p.Localisation;
        MembreDepuis = p.MembreDepuis;
        EconomiesAffichees = "+" + Fcfa.Format(p.EconomiesTrimestre);
        PlafondAffiche = Fcfa.Format(p.BudgetMensuel);
        var seuil = p.BudgetMensuel * p.SeuilAlertePct / 100m;
        SeuilAffiche = $"Seuil : {Fcfa.Format(seuil)}";
        SeuilDetail = $"Seuil d'alerte configuré à {p.SeuilAlertePct}%";
        EngageProgress = p.BudgetMensuel == 0 ? 0 : Math.Min(1d, (double)(depense / p.BudgetMensuel));
        EngageAffiche = $"Engagé : {Fcfa.Format(depense)} ({Fcfa.Percent(EngageProgress * 100)})";

        Marches.Clear();
        foreach (var m in _data.Marches) Marches.Add(m);

    }

    [RelayCommand]
    private Task ModifierProfilAsync() => Shell.Current.GoToAsync(Routes.Parametres);

    [RelayCommand]
    private async Task AjouterMarcheAsync()
    {
        var nom = await Shell.Current.DisplayPromptAsync("Ajouter un marché", "Nom du marché", "Ajouter", "Annuler", "Ex : Marché Mvog-Mbi");
        if (string.IsNullOrWhiteSpace(nom)) return;
        var m = new Marche { Nom = nom.Trim(), Description = "Marché suivi" };
        _data.Marches.Add(m);
        Marches.Add(m);
    }

    [RelayCommand]
    private async Task ExporterAsync()
    {
        try { await ExportHelper.ExportCsvAsync(await _data.GetCoursesAsync()); }
        catch (Exception ex) { await AlertAsync("Export impossible", ex.Message); }
    }

    [RelayCommand]
    private async Task DeconnexionAsync()
    {
        if (!await ConfirmAsync("Déconnexion", "Se déconnecter de cet appareil ?", "Se déconnecter", "Annuler")) return;
        await _auth.LogoutAsync();
        await Shell.Current.GoToAsync(Routes.Login);
    }
}
