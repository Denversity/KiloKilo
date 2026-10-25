using Microsoft.Extensions.DependencyInjection;

namespace KiloKilo;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Restaure le choix de thème (mode sombre basse consommation)
        var dark = Preferences.Default.Get("mode_sombre", false);
        UserAppTheme = dark ? AppTheme.Dark : AppTheme.Unspecified;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());
        // Au retour dans l'application, on tente d'envoyer les modifications en attente
        window.Resumed += (_, _) => IPlatformApplication.Current?.Services.GetService<Services.ISyncService>()?.Demander();
        return window;
    }
}
