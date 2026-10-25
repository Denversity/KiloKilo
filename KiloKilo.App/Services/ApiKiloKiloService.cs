using KiloKilo.Models;
using KiloKilo.Shared;

namespace KiloKilo.Services;

/// <summary>
/// Dépôt « hors ligne d'abord » : toutes les lectures et écritures passent par le cache JSON local ;
/// chaque écriture est placée dans la file d'attente puis envoyée à l'API par le SyncService.
/// Les statistiques et les prix par produit sont calculés localement avec la même logique que l'API (KiloKilo.Shared).
/// </summary>
public class ApiKiloKiloService : IKiloKiloService
{
    private readonly CacheStore _cache;
    private readonly ISyncService _sync;

    public ApiKiloKiloService(CacheStore cache, ISyncService sync)
    {
        _cache = cache;
        _sync = sync;
    }

    public UserProfile Profil => _cache.GetProfil()?.ToModel() ?? new UserProfile();

    public IList<Marche> Marches => _cache.GetMarches().Select(m => m.ToModel()).ToList();

    // ---------------- Courses ----------------

    public Task<IReadOnlyList<Course>> GetCoursesAsync()
    {
        // Les plus récemment ajoutées d'abord à date égale (tri stable après inversion)
        var liste = Enumerable.Reverse(_cache.GetCourses())
            .Select(d =>
            {
                var c = d.ToModel();
                c.EnAttente = _cache.IsCoursePending(d.Id);
                return c;
            })
            .OrderByDescending(c => c.Date)
            .ToList();
        return Task.FromResult<IReadOnlyList<Course>>(liste);
    }

    public Task<Course?> GetCourseAsync(Guid id)
    {
        var d = _cache.GetCourse(id);
        if (d is null) return Task.FromResult<Course?>(null);
        var c = d.ToModel();
        c.EnAttente = _cache.IsCoursePending(id);
        return Task.FromResult<Course?>(c);
    }

    public Task AddCourseAsync(Course course) => EnregistrerCourseAsync(course);

    public Task UpdateCourseAsync(Course course) => EnregistrerCourseAsync(course);

    private async Task EnregistrerCourseAsync(Course course)
    {
        _cache.PutCourse(course.ToDto());
        _cache.Enqueue(OpTypes.CourseUpsert, course.Id);
        await _cache.SaveAsync();
        _sync.Demander();
    }

    public async Task DeleteCourseAsync(Guid id)
    {
        _cache.RemoveCourse(id);
        _cache.Enqueue(OpTypes.CourseDelete, id);
        await _cache.SaveAsync();
        _sync.Demander();
    }

    // ---------------- Prix et statistiques (calculs locaux) ----------------

    public Task<IReadOnlyList<ProduitStat>> GetProduitsAsync()
        => Task.FromResult<IReadOnlyList<ProduitStat>>(
            ProduitCalculator.Calculer(_cache.GetCourses()).Select(p => p.ToModel()).ToList());

    public Task<StatsData> GetStatsAsync(string periode)
        => Task.FromResult(StatsCalculator.Calculer(_cache.GetCourses(), periode, DateOnly.FromDateTime(DateTime.Today)).ToModel());

    // ---------------- Profil et marchés ----------------

    public async Task SaveProfilAsync(UserProfile p)
    {
        var dto = _cache.GetProfil();
        if (dto is null) return;
        dto.NomComplet = p.NomComplet;
        dto.Telephone = p.Telephone;
        dto.Email = string.IsNullOrWhiteSpace(p.Email) ? null : p.Email;
        dto.Localisation = p.Localisation;
        dto.BudgetMensuel = p.BudgetMensuel;
        dto.SeuilAlertePct = p.SeuilAlertePct;

        _cache.SetProfil(dto);
        _cache.Enqueue(OpTypes.ProfilUpdate, Guid.Empty);
        await _cache.SaveAsync();
        _sync.Demander();
    }

    public async Task AddMarcheAsync(Marche marche)
    {
        if (marche.Id == Guid.Empty) marche.Id = Guid.NewGuid();
        _cache.PutMarche(marche.ToDto());
        _cache.Enqueue(OpTypes.MarcheUpsert, marche.Id);
        await _cache.SaveAsync();
        _sync.Demander();
    }
}
