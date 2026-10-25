using KiloKilo.Helpers;
using KiloKilo.Views;

namespace KiloKilo;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Pages « poussées » (hors barre d'onglets), voir l'arborescence de navigation
        Routing.RegisterRoute(Routes.NouvelleCourse, typeof(NouvelleCoursePage)); // Accueil > Nouvelle course (+ modification)
        Routing.RegisterRoute(Routes.DetailCourse, typeof(DetailCoursePage));     // Détails du panier / Validation
        Routing.RegisterRoute(Routes.Produit, typeof(ProduitDetailPage));         // Prix > Évolution d'un produit
        Routing.RegisterRoute(Routes.Parametres, typeof(ParametresPage));         // Profil > Paramètres
        Routing.RegisterRoute(Routes.CodeOublie, typeof(CodeOubliePage));         // Connexion > Code oublié
    }
}
