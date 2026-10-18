using KiloKilo.Services;
using KiloKilo.ViewModels;
using KiloKilo.Views;
using Microsoft.Extensions.Logging;

namespace KiloKilo;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("NunitoSans-Regular.ttf", "NunitoSans");
                fonts.AddFont("NunitoSans-SemiBold.ttf", "NunitoSansSemiBold");
                fonts.AddFont("NunitoSans-Bold.ttf", "NunitoSansBold");
                fonts.AddFont("NunitoSans-ExtraBold.ttf", "NunitoSansExtraBold");
            });

        // Services (données simulées en mémoire : à remplacer par l'API au prochain incrément)
        builder.Services.AddSingleton<IAuthService, MockAuthService>();
        builder.Services.AddSingleton<IKiloKiloService, MockKiloKiloService>();

        // ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<NouvelleCourseViewModel>();
        builder.Services.AddTransient<HistoriqueViewModel>();
        builder.Services.AddTransient<DetailCourseViewModel>();
        builder.Services.AddTransient<StatsViewModel>();
        builder.Services.AddTransient<ProfilViewModel>();
        builder.Services.AddTransient<ProduitsViewModel>();
        builder.Services.AddTransient<ProduitDetailViewModel>();
        builder.Services.AddTransient<ParametresViewModel>();
        builder.Services.AddTransient<CodeOublieViewModel>();

        // Pages
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<NouvelleCoursePage>();
        builder.Services.AddTransient<HistoriquePage>();
        builder.Services.AddTransient<DetailCoursePage>();
        builder.Services.AddTransient<StatsPage>();
        builder.Services.AddTransient<ProfilPage>();
        builder.Services.AddTransient<ProduitsPage>();
        builder.Services.AddTransient<ProduitDetailPage>();
        builder.Services.AddTransient<ParametresPage>();
        builder.Services.AddTransient<CodeOubliePage>();

#if WINDOWS
        KiloKilo.WinUI.CursorHelper.Register(); // curseur « main » sur les éléments cliquables
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
