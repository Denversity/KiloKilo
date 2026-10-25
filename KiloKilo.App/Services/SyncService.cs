using KiloKilo.Shared;

namespace KiloKilo.Services;

public enum SyncStatut { Inactif, EnCours, HorsLigne, ConnexionRequise }

public interface ISyncService
{
    SyncStatut Statut { get; }
    int EnAttente { get; }
    DateTime? DerniereSync { get; }
    string? DerniereErreur { get; }
    /// <summary>Texte d'état à afficher (vide si tout est à jour).</summary>
    string Message { get; }

    /// <summary>Déclenché (sur le thread principal) quand l'état ou les données changent.</summary>
    event Action? Change;

    /// <summary>Synchronisation immédiate : envoie la file d'attente puis récupère les changements du serveur.</summary>
    Task<bool> SyncAsync();

    /// <summary>Demande une synchronisation en arrière-plan (limitée à une toutes les 20 s sauf s'il y a des modifications en attente).</summary>
    void Demander();
}

public class SyncService : ISyncService
{
    private readonly ApiClient _api;
    private readonly CacheStore _cache;
    private readonly SessionStore _session;
    private readonly SemaphoreSlim _verrou = new(1, 1);
    private DateTime _derniereTentative = DateTime.MinValue;

    public SyncService(ApiClient api, CacheStore cache, SessionStore session)
    {
        _api = api;
        _cache = cache;
        _session = session;

        _api.SessionExpired += () => Changer(SyncStatut.ConnexionRequise);
        Connectivity.Current.ConnectivityChanged += (_, e) =>
        {
            if (e.NetworkAccess == NetworkAccess.Internet && Statut != SyncStatut.ConnexionRequise) Demander();
        };
    }

    public SyncStatut Statut { get; private set; } = SyncStatut.Inactif;
    public int EnAttente => _cache.PendingCount;
    public DateTime? DerniereSync => _cache.LastSyncAt;
    public string? DerniereErreur { get; private set; }
    public event Action? Change;

    public string Message => Statut switch
    {
        SyncStatut.EnCours => "Synchronisation en cours…",
        SyncStatut.ConnexionRequise => "Session expirée • touchez pour vous reconnecter",
        SyncStatut.HorsLigne when EnAttente > 0 => $"Hors ligne • {Pluriel(EnAttente)} en attente de synchronisation",
        SyncStatut.HorsLigne => "Hors ligne • données enregistrées sur l'appareil",
        _ when EnAttente > 0 => $"{Pluriel(EnAttente)} en attente de synchronisation",
        _ => string.Empty
    };

    private static string Pluriel(int n) => $"{n} modification{(n > 1 ? "s" : "")}";

    private bool SessionActive =>
        _session.Current is { RefreshToken.Length: > 0 } s && _cache.UserId is not null && s.UserId == _cache.UserId;

    public void Demander()
    {
        if (!SessionActive || Statut == SyncStatut.ConnexionRequise || Statut == SyncStatut.EnCours) return;
        if (EnAttente == 0 && (DateTime.UtcNow - _derniereTentative).TotalSeconds < 20) return;
        _ = Task.Run(SyncAsync);
    }

    public async Task<bool> SyncAsync()
    {
        if (!SessionActive) return false;
        if (!await _verrou.WaitAsync(0)) return false; // déjà en cours

        try
        {
            _derniereTentative = DateTime.UtcNow;
            Changer(SyncStatut.EnCours);

            await PousserAsync();
            await TirerAsync();

            Changer(SyncStatut.Inactif);
            return true;
        }
        catch (ApiOfflineException) { Changer(SyncStatut.HorsLigne); return false; }
        catch (ApiAuthException) { Changer(SyncStatut.ConnexionRequise); return false; }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Synchronisation : {ex}");
            DerniereErreur = ex.Message;
            Changer(SyncStatut.HorsLigne);
            return false;
        }
        finally
        {
            try { await _cache.SaveAsync(); } catch { /* disque plein, etc. : réessayé à la prochaine écriture */ }
            _verrou.Release();
            Notifier();
        }
    }

    // ---------------- Envoi de la file d'attente ----------------

    private async Task PousserAsync()
    {
        var rejets = 0;
        foreach (var op in _cache.GetOutbox())
        {
            try
            {
                switch (op.Type)
                {
                    case OpTypes.CourseUpsert:
                        if (_cache.GetCourse(op.EntityId) is { } c)
                            _cache.ApplyServerCourse(await _api.PutAsync<CourseDto>($"/api/courses/{c.Id}", c), op.Id);
                        break;

                    case OpTypes.CourseDelete:
                        await _api.DeleteAsync($"/api/courses/{op.EntityId}");
                        break;

                    case OpTypes.MarcheUpsert:
                        if (_cache.GetMarche(op.EntityId) is { } m)
                            await _api.PutAsync<MarcheDto>($"/api/marches/{m.Id}", m);
                        break;

                    case OpTypes.MarcheDelete:
                        await _api.DeleteAsync($"/api/marches/{op.EntityId}");
                        break;

                    case OpTypes.ProfilUpdate:
                        if (_cache.GetProfil() is { } p)
                        {
                            var res = await _api.PutAsync<ProfilDto>("/api/profil", p);
                            if (!_cache.GetOutbox().Any(o => o.Type == OpTypes.ProfilUpdate && o.Id != op.Id)) _cache.SetProfil(res);
                        }
                        break;
                }
                _cache.CompleteOp(op.Id);
                await _cache.SaveAsync();
            }
            catch (ApiException ex) when (ex is not ApiAuthException && ex.Status is >= 400 and < 500)
            {
                // Refusée définitivement (validation, conflit…) : on l'abandonne pour ne pas bloquer la file
                rejets++;
                DerniereErreur = ex.Message;
                _cache.CompleteOp(op.Id);
                await _cache.SaveAsync();
            }
        }
        if (rejets == 0) DerniereErreur = null;
    }

    // ---------------- Récupération des changements du serveur ----------------

    private async Task TirerAsync()
    {
        var reponse = await _api.GetAsync<SyncCoursesResponse>($"/api/courses/sync?since={_cache.LastSync}");
        _cache.MergeCourses(reponse.Courses);
        _cache.ReplaceMarchesFromServer(await _api.GetAsync<List<MarcheDto>>("/api/marches"));
        _cache.ReplaceProfilFromServer(await _api.GetAsync<ProfilDto>("/api/profil"));
        _cache.MarkSynced(reponse.ServerTime);
    }

    private void Changer(SyncStatut statut)
    {
        if (Statut == statut) return;
        Statut = statut;
        Notifier();
    }

    private void Notifier() => MainThread.BeginInvokeOnMainThread(() => Change?.Invoke());
}
