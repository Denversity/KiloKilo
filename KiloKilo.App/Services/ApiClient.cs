using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using KiloKilo.Shared;

namespace KiloKilo.Services;

/// <summary>
/// Client HTTP de l'API KiloKilo : jeton Bearer, rafraîchissement automatique sur 401 (une seule tentative),
/// erreurs typées (hors ligne / refus du serveur / session expirée).
/// </summary>
public class ApiClient
{
    private readonly ApiSettings _settings;
    private readonly SessionStore _session;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public ApiClient(ApiSettings settings, SessionStore session)
    {
        _settings = settings;
        _session = session;
    }

    /// <summary>Levé quand le serveur refuse définitivement le jeton de rafraîchissement.</summary>
    public event Action? SessionExpired;

    public async Task<T> GetAsync<T>(string path, bool auth = true) => Lire<T>(await EnvoyerAsync(HttpMethod.Get, path, null, auth));
    public async Task<T> PostAsync<T>(string path, object? body, bool auth = true) => Lire<T>(await EnvoyerAsync(HttpMethod.Post, path, body, auth));
    public async Task<T> PutAsync<T>(string path, object? body) => Lire<T>(await EnvoyerAsync(HttpMethod.Put, path, body, true));
    public Task PostAsync(string path, object? body, bool auth = true) => EnvoyerAsync(HttpMethod.Post, path, body, auth);
    public Task DeleteAsync(string path) => EnvoyerAsync(HttpMethod.Delete, path, null, true);

    /// <summary>Teste l'adresse d'un serveur (GET /health), sans authentification.</summary>
    public async Task<bool> TesterAsync(string baseUrl)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
            using var r = await _http.GetAsync(ApiSettings.Normaliser(baseUrl) + "/health", cts.Token);
            return r.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    private static T Lire<T>(string texte)
        => JsonSerializer.Deserialize<T>(texte, Json.Options) ?? throw new ApiException(500, "Réponse du serveur invalide.");

    private async Task<string> EnvoyerAsync(HttpMethod method, string path, object? body, bool auth)
    {
        for (var tentative = 0; ; tentative++)
        {
            if (auth) await AssurerJetonAsync();
            var jetonUtilise = _session.Current?.AccessToken;

            using var req = new HttpRequestMessage(method, _settings.BaseUrl + path);
            if (body is not null) req.Content = JsonContent.Create(body, body.GetType(), null, Json.Options);
            if (auth && !string.IsNullOrEmpty(jetonUtilise)) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jetonUtilise);

            HttpResponseMessage resp;
            try { resp = await _http.SendAsync(req); }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException or IOException)
            {
                throw new ApiOfflineException(ex);
            }

            using (resp)
            {
                var texte = await resp.Content.ReadAsStringAsync();
                if (resp.IsSuccessStatusCode) return texte;

                if ((int)resp.StatusCode == 401 && auth && tentative == 0)
                {
                    if (await RafraichirAsync(jetonUtilise)) continue;
                    throw new ApiAuthException();
                }
                throw new ApiException((int)resp.StatusCode, ExtraireMessage(texte, (int)resp.StatusCode));
            }
        }
    }

    private async Task AssurerJetonAsync()
    {
        var s = _session.Current;
        if (s is null || string.IsNullOrEmpty(s.RefreshToken)) throw new ApiAuthException();
        var marge = DateTimeOffset.UtcNow.AddSeconds(30).ToUnixTimeMilliseconds();
        if (s.AccessExpiresAt > marge && !string.IsNullOrEmpty(s.AccessToken)) return;
        if (!await RafraichirAsync(null)) throw new ApiAuthException();
    }

    /// <param name="jetonUtilise">Jeton de la requête refusée ; s'il a changé entre-temps, un autre appel a déjà rafraîchi.</param>
    private async Task<bool> RafraichirAsync(string? jetonUtilise)
    {
        await _refreshLock.WaitAsync();
        try
        {
            var s = _session.Current;
            if (s is null || string.IsNullOrEmpty(s.RefreshToken)) return false;

            if (jetonUtilise is not null && s.AccessToken != jetonUtilise) return true;
            var marge = DateTimeOffset.UtcNow.AddSeconds(30).ToUnixTimeMilliseconds();
            if (jetonUtilise is null && s.AccessExpiresAt > marge && !string.IsNullOrEmpty(s.AccessToken)) return true;

            try
            {
                var texte = await EnvoyerAsync(HttpMethod.Post, "/api/auth/refresh", new RefreshRequest(s.RefreshToken), false);
                var r = Lire<AuthResponse>(texte);
                s.AccessToken = r.AccessToken;
                s.RefreshToken = r.RefreshToken;
                s.AccessExpiresAt = r.ExpiresAt;
                s.Expiree = false;
                await _session.SaveAsync();
                return true;
            }
            catch (ApiException ex) when (ex.Status is 400 or 401)
            {
                s.Expiree = true;
                await _session.SaveAsync();
                SessionExpired?.Invoke();
                return false;
            }
        }
        finally { _refreshLock.Release(); }
    }

    private static string ExtraireMessage(string texte, int status)
    {
        try
        {
            using var doc = JsonDocument.Parse(texte);
            foreach (var cle in new[] { "detail", "title" })
                if (doc.RootElement.TryGetProperty(cle, out var v) && v.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(v.GetString()))
                    return v.GetString()!;
        }
        catch { /* corps non JSON */ }
        return $"Erreur du serveur ({status}).";
    }
}
