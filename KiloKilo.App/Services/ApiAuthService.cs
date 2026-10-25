using KiloKilo.Shared;

namespace KiloKilo.Services;

public class ApiAuthService : IAuthService
{
    private readonly ApiClient _api;
    private readonly SessionStore _session;
    private readonly CacheStore _cache;
    private readonly ISyncService _sync;

    public ApiAuthService(ApiClient api, SessionStore session, CacheStore cache, ISyncService sync)
    {
        _api = api;
        _session = session;
        _cache = cache;
        _sync = sync;
    }

    public bool IsAuthenticated => _session.Current is not null && _cache.UserId is not null;

    public async Task<bool> TryRestoreSessionAsync()
    {
        await _session.LoadAsync();
        var s = _session.Current;
        if (s is null || !s.Memoriser) return false;

        await _cache.LoadAsync(s.UserId);
        _sync.Demander();
        return true;
    }

    public async Task<AuthResult> LoginAsync(string identifiant, string pin, bool memoriser)
    {
        try
        {
            var r = await _api.PostAsync<AuthResponse>("/api/auth/login", new LoginRequest(identifiant.Trim(), pin), auth: false);
            await InstallerAsync(r, identifiant, pin, memoriser);
            await _sync.SyncAsync();
            return new AuthResult(true);
        }
        catch (ApiOfflineException) { return await LoginHorsLigneAsync(identifiant, pin, memoriser); }
        catch (ApiException ex) { return new AuthResult(false, ex.Message); }
    }

    private async Task<AuthResult> LoginHorsLigneAsync(string identifiant, string pin, bool memoriser)
    {
        await _session.LoadAsync();
        var s = _session.Current;
        if (s is null)
            return new AuthResult(false, "Serveur injoignable. Une première connexion avec Internet est nécessaire sur cet appareil.");

        var id = IdentifiantHelper.Normaliser(identifiant);
        if (!s.IdentifiantsHorsLigne.Contains(id) || !PinLocal.Verifier(pin, s.PinSel, s.PinEmpreinte))
            return new AuthResult(false, "Serveur injoignable et identifiants non reconnus sur cet appareil.");

        s.Memoriser = memoriser;
        await _session.SetAsync(s);
        await _cache.LoadAsync(s.UserId);
        return new AuthResult(true, null, HorsLigne: true);
    }

    public async Task<AuthResult> OfflineDirectAsync()
    {
        await _session.LoadAsync();
        var s = _session.Current;
        if (s is null || !s.Memoriser)
            return new AuthResult(false, "Connectez-vous une première fois en cochant « Mémoriser sur cet appareil » pour utiliser ce mode.");

        await _cache.LoadAsync(s.UserId);
        _sync.Demander();
        return new AuthResult(true, null, HorsLigne: true);
    }

    public async Task<AuthResult> RegisterAsync(string nom, string telephone, string marche, decimal? budget, string pin)
    {
        try
        {
            var r = await _api.PostAsync<AuthResponse>("/api/auth/register",
                new RegisterRequest(nom, telephone, marche, budget, pin), auth: false);
            await InstallerAsync(r, telephone, pin, memoriser: true);
            await _sync.SyncAsync();
            return new AuthResult(true);
        }
        catch (ApiOfflineException) { return new AuthResult(false, "Serveur injoignable : la création d'un compte nécessite une connexion Internet."); }
        catch (ApiException ex) { return new AuthResult(false, ex.Message); }
    }

    /// <summary>Enregistre la session et prépare le cache de l'utilisateur (sans perdre ses modifications en attente).</summary>
    private async Task InstallerAsync(AuthResponse r, string identifiantSaisi, string pin, bool memoriser)
    {
        await _session.LoadAsync();
        var ancien = _session.Current;

        // Autre utilisateur sur cet appareil : ses données locales sont effacées
        if (ancien is not null && ancien.UserId != r.Profil.Id)
            await _cache.SupprimerDonneesAsync(ancien.UserId);

        await _cache.LoadAsync(r.Profil.Id);
        if (!_cache.GetOutbox().Any(o => o.Type == OpTypes.ProfilUpdate)) _cache.SetProfil(r.Profil);

        var (sel, empreinte) = PinLocal.Hacher(pin);
        var ids = new List<string> { IdentifiantHelper.Normaliser(identifiantSaisi), IdentifiantHelper.Normaliser(r.Profil.Telephone) };
        if (!string.IsNullOrWhiteSpace(r.Profil.Email)) ids.Add(IdentifiantHelper.Normaliser(r.Profil.Email));

        await _session.SetAsync(new SessionInfo
        {
            AccessToken = r.AccessToken,
            RefreshToken = r.RefreshToken,
            AccessExpiresAt = r.ExpiresAt,
            UserId = r.Profil.Id,
            Memoriser = memoriser,
            IdentifiantsHorsLigne = ids.Distinct().ToList(),
            PinSel = sel,
            PinEmpreinte = empreinte
        });
        await _cache.SaveAsync();
    }

    public async Task LogoutAsync()
    {
        var s = _session.Current;
        if (s is not null && !string.IsNullOrEmpty(s.RefreshToken))
        {
            try { await _api.PostAsync("/api/auth/logout", new RefreshRequest(s.RefreshToken), auth: false); }
            catch { /* hors ligne : le jeton expirera de lui-même */ }
        }
        if (s is not null) await _cache.SupprimerDonneesAsync(s.UserId);
        _cache.Decharger();
        await _session.ClearAsync();
    }

    public async Task<(bool Ok, string Message, string? CodeDeTest)> ForgotAsync(string identifiant)
    {
        try
        {
            var r = await _api.PostAsync<ForgotResponse>("/api/auth/forgot", new ForgotRequest(identifiant.Trim()), auth: false);
            return (true, r.Message, r.CodeDeTest);
        }
        catch (ApiOfflineException) { return (false, "Serveur injoignable : connectez-vous à Internet pour réinitialiser votre code.", null); }
        catch (ApiException ex) { return (false, ex.Message, null); }
    }

    public async Task<AuthResult> ResetAsync(string identifiant, string code, string nouveauPin)
    {
        try
        {
            await _api.PostAsync("/api/auth/reset", new ResetRequest(identifiant.Trim(), code.Trim(), nouveauPin), auth: false);
            return new AuthResult(true);
        }
        catch (ApiOfflineException) { return new AuthResult(false, "Serveur injoignable : connectez-vous à Internet."); }
        catch (ApiException ex) { return new AuthResult(false, ex.Message); }
    }
}
