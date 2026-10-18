using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace KiloKilo.ViewModels;

public partial class CodeOublieViewModel : BaseViewModel
{
    [ObservableProperty] private string identifiant = string.Empty;
    [ObservableProperty] private string erreur = string.Empty;
    [ObservableProperty] private bool envoye;

    public bool HasErreur => !string.IsNullOrEmpty(Erreur);
    public bool NonEnvoye => !Envoye;
    partial void OnErreurChanged(string value) => OnPropertyChanged(nameof(HasErreur));
    partial void OnEnvoyeChanged(bool value) => OnPropertyChanged(nameof(NonEnvoye));

    [RelayCommand]
    private async Task EnvoyerAsync()
    {
        Erreur = string.Empty;
        if (string.IsNullOrWhiteSpace(Identifiant)) { Erreur = "Saisissez votre téléphone ou votre e-mail."; return; }

        IsBusy = true;
        try
        {
            await Task.Delay(500); // Simulation : l'envoi réel du code arrivera avec le backend
            Envoye = true;
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private Task RetourAsync() => Shell.Current.GoToAsync("..");
}
