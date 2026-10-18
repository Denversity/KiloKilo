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
        => new Window(new AppShell());
}
