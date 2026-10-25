using System.Text.Json;
using KiloKilo.Shared;

namespace KiloKilo.Services;

public static class OpTypes
{
    public const string CourseUpsert = "course.upsert";
    public const string CourseDelete = "course.delete";
    public const string MarcheUpsert = "marche.upsert";
    public const string MarcheDelete = "marche.delete";
    public const string ProfilUpdate = "profil.update";
}

/// <summary>Opération locale en attente d'envoi au serveur (file d'attente persistée).</summary>
public class OutboxOp
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public DateTime CreeLe { get; set; } = DateTime.UtcNow;
}

public class CacheMeta
{
    public long LastSync { get; set; }
    public DateTime? LastSyncAt { get; set; }
}

/// <summary>
/// Cache local persistant en JSON, un dossier par utilisateur :
/// courses.json, marches.json, profil.json, outbox.json (modifications à envoyer), meta.json.
/// Les écritures sont atomiques (fichier temporaire puis remplacement) et les accès mémoire sont protégés par un verrou.
/// </summary>
public class CacheStore
{
    private readonly object _gate = new();
    private readonly SemaphoreSlim _io = new(1, 1);

    private List<CourseDto> _courses = new();
    private List<MarcheDto> _marches = new();
    private ProfilDto? _profil;
    private List<OutboxOp> _outbox = new();
    private CacheMeta _meta = new();

    public Guid? UserId { get; private set; }

    private static string Racine => Path.Combine(FileSystem.AppDataDirectory, "cache");
    private static string Dossier(Guid id) => Path.Combine(Racine, id.ToString("N"));

    // ---------------- Chargement / enregistrement ----------------

    public async Task LoadAsync(Guid userId)
    {
        await _io.WaitAsync();
        try
        {
            var dir = Dossier(userId);
            var courses = await LireAsync<List<CourseDto>>(dir, "courses.json") ?? new();
            var marches = await LireAsync<List<MarcheDto>>(dir, "marches.json") ?? new();
            var profil = await LireAsync<ProfilDto>(dir, "profil.json");
            var outbox = await LireAsync<List<OutboxOp>>(dir, "outbox.json") ?? new();
            var meta = await LireAsync<CacheMeta>(dir, "meta.json") ?? new();
            lock (_gate)
            {
                UserId = userId; _courses = courses; _marches = marches; _profil = profil; _outbox = outbox; _meta = meta;
            }
        }
        finally { _io.Release(); }
    }

    public async Task SaveAsync()
    {
        await _io.WaitAsync();
        try
        {
            string[] json;
            Guid uid;
            lock (_gate)
            {
                if (UserId is null) return;
                uid = UserId.Value;
                json = new[]
                {
                    JsonSerializer.Serialize(_courses, Json.Options), JsonSerializer.Serialize(_marches, Json.Options),
                    JsonSerializer.Serialize(_profil, Json.Options), JsonSerializer.Serialize(_outbox, Json.Options),
                    JsonSerializer.Serialize(_meta, Json.Options)
                };
            }
            var dir = Dossier(uid);
            Directory.CreateDirectory(dir);
            var noms = new[] { "courses.json", "marches.json", "profil.json", "outbox.json", "meta.json" };
            for (var i = 0; i < noms.Length; i++) await EcrireAsync(dir, noms[i], json[i]);
        }
        finally { _io.Release(); }
    }

    /// <summary>Libère la mémoire sans toucher aux fichiers (changement d'utilisateur / déconnexion).</summary>
    public void Decharger()
    {
        lock (_gate)
        {
            UserId = null; _courses = new(); _marches = new(); _profil = null; _outbox = new(); _meta = new();
        }
    }

    public async Task SupprimerDonneesAsync(Guid userId)
    {
        await _io.WaitAsync();
        try
        {
            var dir = Dossier(userId);
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
        catch { /* ignoré */ }
        finally { _io.Release(); }
    }

    private static async Task<T?> LireAsync<T>(string dir, string fichier)
    {
        var chemin = Path.Combine(dir, fichier);
        if (!File.Exists(chemin)) return default;
        try { return JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(chemin), Json.Options); }
        catch { return default; } // fichier corrompu : reconstruit par la prochaine synchronisation
    }

    private static async Task EcrireAsync(string dir, string fichier, string contenu)
    {
        var chemin = Path.Combine(dir, fichier);
        var tmp = chemin + ".tmp";
        await File.WriteAllTextAsync(tmp, contenu);
        File.Move(tmp, chemin, true);
    }

    // ---------------- Courses ----------------

    public List<CourseDto> GetCourses() { lock (_gate) return _courses.ToList(); }

    public CourseDto? GetCourse(Guid id) { lock (_gate) return _courses.FirstOrDefault(c => c.Id == id); }

    public void PutCourse(CourseDto dto)
    {
        lock (_gate)
        {
            var i = _courses.FindIndex(c => c.Id == dto.Id);
            if (i >= 0) _courses[i] = dto; else _courses.Add(dto);
        }
    }

    public void RemoveCourse(Guid id) { lock (_gate) _courses.RemoveAll(c => c.Id == id); }

    /// <summary>Applique la réponse du serveur à une course envoyée, sauf si elle a été modifiée de nouveau depuis.</summary>
    public void ApplyServerCourse(CourseDto dto, Guid opId)
    {
        lock (_gate)
        {
            if (_outbox.Any(o => o.EntityId == dto.Id && o.Id != opId)) return;
            var i = _courses.FindIndex(c => c.Id == dto.Id);
            if (i >= 0) _courses[i] = dto; else _courses.Add(dto);
        }
    }

    /// <summary>Fusionne les changements du serveur ; les entités ayant une modification locale en attente sont préservées.</summary>
    public void MergeCourses(IEnumerable<CourseDto> depuisServeur)
    {
        lock (_gate)
        {
            foreach (var dto in depuisServeur)
            {
                if (_outbox.Any(o => o.EntityId == dto.Id)) continue;
                _courses.RemoveAll(c => c.Id == dto.Id);
                if (!dto.Supprime) _courses.Add(dto);
            }
        }
    }

    // ---------------- Marchés & profil ----------------

    public List<MarcheDto> GetMarches() { lock (_gate) return _marches.ToList(); }

    public MarcheDto? GetMarche(Guid id) { lock (_gate) return _marches.FirstOrDefault(m => m.Id == id); }

    public void PutMarche(MarcheDto dto)
    {
        lock (_gate)
        {
            if (dto.EstPrincipal) foreach (var m in _marches) m.EstPrincipal = false;
            var i = _marches.FindIndex(m => m.Id == dto.Id);
            if (i >= 0) _marches[i] = dto; else _marches.Add(dto);
        }
    }

    public void RemoveMarche(Guid id) { lock (_gate) _marches.RemoveAll(m => m.Id == id); }

    public void ReplaceMarchesFromServer(List<MarcheDto> liste)
    {
        lock (_gate)
        {
            if (_outbox.Any(o => o.Type is OpTypes.MarcheUpsert or OpTypes.MarcheDelete)) return;
            _marches = liste;
        }
    }

    public ProfilDto? GetProfil() { lock (_gate) return _profil is null ? null : Clone(_profil); }

    public void SetProfil(ProfilDto dto) { lock (_gate) _profil = dto; }

    public void ReplaceProfilFromServer(ProfilDto dto)
    {
        lock (_gate)
        {
            if (_outbox.Any(o => o.Type == OpTypes.ProfilUpdate)) return;
            _profil = dto;
        }
    }

    private static ProfilDto Clone(ProfilDto p) => new()
    {
        Id = p.Id, NomComplet = p.NomComplet, Telephone = p.Telephone, Email = p.Email, Localisation = p.Localisation,
        BudgetMensuel = p.BudgetMensuel, SeuilAlertePct = p.SeuilAlertePct, MembreDepuis = p.MembreDepuis
    };

    // ---------------- File d'attente ----------------

    /// <summary>Ajoute une opération en fusionnant les anciennes opérations de la même entité.</summary>
    public void Enqueue(string type, Guid entityId)
    {
        lock (_gate)
        {
            bool Famille(OutboxOp o) => o.EntityId == entityId && (
                (type.StartsWith("course") && o.Type.StartsWith("course")) ||
                (type.StartsWith("marche") && o.Type.StartsWith("marche")) ||
                (type.StartsWith("profil") && o.Type.StartsWith("profil")));
            _outbox.RemoveAll(Famille);
            _outbox.Add(new OutboxOp { Type = type, EntityId = entityId });
        }
    }

    public List<OutboxOp> GetOutbox() { lock (_gate) return _outbox.ToList(); }

    public void CompleteOp(Guid opId) { lock (_gate) _outbox.RemoveAll(o => o.Id == opId); }

    public int PendingCount { get { lock (_gate) return _outbox.Count; } }

    public bool IsCoursePending(Guid id) { lock (_gate) return _outbox.Any(o => o.EntityId == id && o.Type.StartsWith("course")); }

    // ---------------- Métadonnées de synchronisation ----------------

    public long LastSync { get { lock (_gate) return _meta.LastSync; } }
    public DateTime? LastSyncAt { get { lock (_gate) return _meta.LastSyncAt; } }

    public void MarkSynced(long serverTime)
    {
        lock (_gate) { _meta.LastSync = serverTime; _meta.LastSyncAt = DateTime.Now; }
    }
}
