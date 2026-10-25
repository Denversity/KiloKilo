namespace KiloKilo.Helpers;

/// <summary>Routes Shell, alignées sur l'arborescence de navigation KiloKilo.</summary>
public static class Routes
{
    // Authentification
    public const string Login = "//login";
    public const string Register = "//register";
    public const string CodeOublie = "codeoublie";

    // Application (onglets)
    public const string Accueil = "//main/home";
    public const string Historique = "//main/historique";
    public const string Prix = "//main/prix";
    public const string Stats = "//main/stats";
    public const string Profil = "//main/profil";

    // Pages poussées (hors barre d'onglets)
    public const string NouvelleCourse = "nouvellecourse";
    public const string DetailCourse = "detailcourse";
    public const string Produit = "produit";
    public const string Parametres = "parametres";
}
