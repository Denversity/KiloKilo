using KiloKilo.Services;
using Microsoft.Extensions.DependencyInjection;
using KiloKilo.ViewModels;

namespace KiloKilo.Views;

/// <summary>
/// Page de base responsive :
///  - centre le contenu et limite sa largeur à MaxContentWidth en ajoutant des marges latérales
///    au conteneur HostView (le conteneur reste en « Fill », donc jamais décalé) ;
///  - bascule l'état visuel « Wide » / « Compact » du conteneur selon la largeur de la fenêtre ;
///  - déclenche le rafraîchissement du ViewModel à chaque affichage.
/// </summary>
public abstract class ResponsivePage : ContentPage
{
    private Thickness? _basePadding;

    /// <summary>Largeur maximale du contenu (modifiable en XAML : MaxContentWidth="960").</summary>
    public double MaxContentWidth { get; set; } = 640;

    /// <summary>Largeur de fenêtre à partir de laquelle on passe en mise en page « Wide » (2 colonnes).</summary>
    public double WideBreakpoint { get; set; } = 840;

    /// <summary>Conteneur racine du contenu (x:Name="Host").</summary>
    protected View? HostView { get; set; }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (HostView is not Layout host || width <= 0) return;

        host.HorizontalOptions = LayoutOptions.Fill;
        _basePadding ??= host.Padding;
        var b = _basePadding.Value;

        var extra = Math.Max(0, (width - MaxContentWidth) / 2);
        var target = new Thickness(b.Left + extra, b.Top, b.Right + extra, b.Bottom);
        if (!host.Padding.Equals(target))
            host.Padding = target;

        VisualStateManager.GoToState(host, width >= WideBreakpoint ? "Wide" : "Compact");
    }

    private ISyncService? _sync;

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // À chaque changement de synchronisation (fin de synchro, passage hors ligne…), la page se met à jour
        _sync ??= IPlatformApplication.Current?.Services.GetService<ISyncService>();
        if (_sync is not null)
        {
            _sync.Change -= OnSyncChange;
            _sync.Change += OnSyncChange;
            _sync.Demander();
        }

        if (BindingContext is BaseViewModel vm)
            await vm.OnAppearingAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_sync is not null) _sync.Change -= OnSyncChange;
    }

    private async void OnSyncChange()
    {
        if (BindingContext is BaseViewModel vm)
        {
            try { await vm.OnAppearingAsync(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        }
    }
}
