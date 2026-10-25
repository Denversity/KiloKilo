using CommunityToolkit.Mvvm.ComponentModel;

namespace KiloKilo.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool isBusy;

    public bool IsNotBusy => !IsBusy;

    /// <summary>Appelé à chaque affichage de la page (rafraîchissement des données).</summary>
    public virtual Task OnAppearingAsync() => Task.CompletedTask;

    protected static Task AlertAsync(string titre, string message)
        => Shell.Current.DisplayAlert(titre, message, "OK");

    protected static Task<bool> ConfirmAsync(string titre, string message, string accepter = "Oui", string annuler = "Non")
        => Shell.Current.DisplayAlert(titre, message, accepter, annuler);
}
