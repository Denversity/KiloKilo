using KiloKilo.Models;

namespace KiloKilo.Services;

/// <summary>
/// Accès aux données du carnet d'approvisionnement. L'implémentation lit et écrit d'abord le cache local (JSON),
/// puis la synchronisation avec l'API se fait en arrière-plan (voir ISyncService).
/// </summary>
public interface IKiloKiloService
{
    UserProfile Profil { get; }
    IList<Marche> Marches { get; }

    Task<IReadOnlyList<Course>> GetCoursesAsync();
    Task<Course?> GetCourseAsync(Guid id);
    Task AddCourseAsync(Course course);
    Task UpdateCourseAsync(Course course);
    Task DeleteCourseAsync(Guid id);

    Task<IReadOnlyList<ProduitStat>> GetProduitsAsync();
    Task<StatsData> GetStatsAsync(string periode);

    Task SaveProfilAsync(UserProfile profil);
    Task AddMarcheAsync(Marche marche);
}
