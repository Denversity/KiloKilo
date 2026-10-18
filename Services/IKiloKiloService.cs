using KiloKilo.Models;

namespace KiloKilo.Services;

/// <summary>
/// Accès aux données du carnet d'approvisionnement.
/// Implémentation actuelle : en mémoire (MockKiloKiloService). Le backend remplacera cette implémentation sans toucher aux ViewModels.
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

    /// <summary>Historique de prix par produit (reconstitué depuis les courses).</summary>
    Task<IReadOnlyList<ProduitStat>> GetProduitsAsync();

    Task<StatsData> GetStatsAsync(string periode);
}
