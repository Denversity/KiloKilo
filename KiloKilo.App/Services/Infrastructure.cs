using System.Security.Cryptography;
using System.Text.Json;

namespace KiloKilo.Services;

public static class Json
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}

/// <summary>Erreur renvoyée par le serveur (validation, conflit, etc.). Le message est affichable tel quel.</summary>
public class ApiException : Exception
{
    public int Status { get; }
    public ApiException(int status, string message) : base(message) => Status = status;
}

/// <summary>Session expirée : le jeton de rafraîchissement n'est plus accepté.</summary>
public class ApiAuthException : ApiException
{
    public ApiAuthException() : base(401, "Session expirée. Reconnectez-vous.") { }
}

/// <summary>Serveur injoignable (hors ligne, délai dépassé…).</summary>
public class ApiOfflineException : Exception
{
    public ApiOfflineException(Exception? inner = null) : base("Serveur injoignable.", inner) { }
}

/// <summary>Adresse du serveur, modifiable dans l'application (Connexion et Paramètres).</summary>
public class ApiSettings
{
    private const string Key = "api_url";

    /// <summary>10.0.2.2 = machine hôte vue depuis l'émulateur Android.</summary>
    public static string UrlParDefaut =>
        DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5080" : "http://localhost:5080";

    public string BaseUrl
    {
        get => Normaliser(Preferences.Default.Get(Key, UrlParDefaut));
        set => Preferences.Default.Set(Key, Normaliser(value));
    }

    public static string Normaliser(string? valeur)
    {
        var v = (valeur ?? string.Empty).Trim().TrimEnd('/');
        if (v.Length == 0) return UrlParDefaut;
        return v.Contains("://") ? v : "http://" + v;
    }
}

public static class IdentifiantHelper
{
    /// <summary>Même normalisation que le serveur : chiffres et « + » pour un téléphone, minuscules pour un e-mail.</summary>
    public static string Normaliser(string? valeur)
    {
        var v = (valeur ?? string.Empty).Trim();
        return v.Contains('@') ? v.ToLowerInvariant() : new string(v.Where(c => char.IsDigit(c) || c == '+').ToArray());
    }
}

/// <summary>Vérification du PIN hors ligne (empreinte salée stockée dans le stockage sécurisé, jamais le PIN).</summary>
public static class PinLocal
{
    public static (string Sel, string Empreinte) Hacher(string pin)
    {
        var sel = RandomNumberGenerator.GetBytes(16);
        var h = Rfc2898DeriveBytes.Pbkdf2(pin, sel, 100_000, HashAlgorithmName.SHA256, 32);
        return (Convert.ToBase64String(sel), Convert.ToBase64String(h));
    }

    public static bool Verifier(string pin, string? sel, string? empreinte)
    {
        if (string.IsNullOrEmpty(sel) || string.IsNullOrEmpty(empreinte)) return false;
        var attendu = Convert.FromBase64String(empreinte);
        var h = Rfc2898DeriveBytes.Pbkdf2(pin, Convert.FromBase64String(sel), 100_000, HashAlgorithmName.SHA256, attendu.Length);
        return CryptographicOperations.FixedTimeEquals(h, attendu);
    }
}

public class SessionInfo
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public long AccessExpiresAt { get; set; }
    public Guid UserId { get; set; }
    public bool Memoriser { get; set; }
    public bool Expiree { get; set; }
    public List<string> IdentifiantsHorsLigne { get; set; } = new();
    public string? PinSel { get; set; }
    public string? PinEmpreinte { get; set; }
}

/// <summary>Jetons et identité de l'appareil, conservés dans le stockage sécurisé du système.</summary>
public class SessionStore
{
    private const string Key = "kilokilo_session";
    private bool _charge;

    public SessionInfo? Current { get; private set; }

    public async Task LoadAsync()
    {
        if (_charge) return;
        _charge = true;
        string? json;
        try { json = await SecureStorage.Default.GetAsync(Key); }
        catch { json = Preferences.Default.Get(Key, string.Empty); } // émulateur / appareil sans coffre
        if (string.IsNullOrEmpty(json)) return;
        try { Current = JsonSerializer.Deserialize<SessionInfo>(json, Json.Options); } catch { Current = null; }
    }

    public async Task SetAsync(SessionInfo session)
    {
        Current = session;
        _charge = true;
        var json = JsonSerializer.Serialize(session, Json.Options);
        try { await SecureStorage.Default.SetAsync(Key, json); }
        catch { Preferences.Default.Set(Key, json); }
    }

    /// <summary>Persiste l'état courant (après modification d'un champ de Current).</summary>
    public Task SaveAsync() => Current is null ? Task.CompletedTask : SetAsync(Current);

    public Task ClearAsync()
    {
        Current = null;
        try { SecureStorage.Default.Remove(Key); } catch { /* ignoré */ }
        Preferences.Default.Remove(Key);
        return Task.CompletedTask;
    }
}
