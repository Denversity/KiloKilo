using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KiloKilo.Services;

namespace KiloKilo.ViewModels;

public partial class CodeOublieViewModel : BaseViewModel
{
    private readonly IAuthService _auth;

    public CodeOublieViewModel(IAuthService auth) => _auth = auth;

    [ObservableProperty] private string identifiant = string.Empty;
    [ObservableProperty] private string code = string.Empty;
    [ObservableProperty] private string nouveauPin = string.Empty;
    [ObservableProperty] private string erreur = string.Empty;
    [ObservableProperty] private string message = string.Empty;
    [ObservableProperty] private string codeTest = string.Empty;
    [ObservableProperty] private bool etape2;

    public bool HasErreur => !string.IsNullOrEmpty(Erreur);
    public bool HasCodeTest => !string.IsNullOrEmpty(CodeTest);
    public bool Etape1 => !Etape2;
    partial void OnErreurChanged(string value) => OnPropertyChanged(nameof(HasErreur));
    partial void OnCodeTestChanged(string value) => OnPropertyChanged(nameof(HasCodeTest));
    partial void OnEtape2Changed(bool value) => OnPropertyChanged(nameof(Etape1));

    [RelayCommand]
    private async Task EnvoyerAsync()
    {
        Erreur = string.Empty;
        if (string.IsNullOrWhiteSpace(Identifiant)) { Erreur = "Saisissez votre téléphone ou votre e-mail."; return; }

        IsBusy = true;
        try
        {
            var (ok, msg, test) = await _auth.ForgotAsync(Identifiant);
            if (!ok) { Erreur = msg; return; }
            Message = msg;
            // Le serveur ne renvoie le code que en développement (aucun SMS n'est encore branché)
            CodeTest = string.IsNullOrEmpty(test) ? string.Empty : $"Code de test (développement) : {test}";
            Etape2 = true;
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ReinitialiserAsync()
    {
        Erreur = string.Empty;
        if (Code.Trim().Length != 6) { Erreur = "Saisissez le code à 6 chiffres reçu."; return; }
        if (NouveauPin.Length != 4 || !NouveauPin.All(char.IsDigit)) { Erreur = "Le nouveau code secret comporte 4 chiffres."; return; }

        IsBusy = true;
        try
        {
            var r = await _auth.ResetAsync(Identifiant, Code, NouveauPin);
            if (!r.Ok) { Erreur = r.Erreur ?? "Réinitialisation impossible."; return; }
            await AlertAsync("Code secret modifié", "Vous pouvez vous connecter avec votre nouveau code.");
            await Shell.Current.GoToAsync("..");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private Task RetourAsync() => Shell.Current.GoToAsync("..");
}
