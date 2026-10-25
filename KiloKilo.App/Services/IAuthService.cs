namespace KiloKilo.Services;

public record AuthResult(bool Ok, string? Erreur = null, bool HorsLigne = false);

public interface IAuthService
{
    bool IsAuthenticated { get; }

    /// <summary>Rouvre la session mémorisée sur l'appareil (démarrage de l'application).</summary>
    Task<bool> TryRestoreSessionAsync();

    Task<AuthResult> LoginAsync(string identifiant, string pin, bool memoriser);

    /// <summary>« Mode hors-ligne direct (au marché) » : réutilise la session mémorisée sans contacter le serveur.</summary>
    Task<AuthResult> OfflineDirectAsync();

    Task<AuthResult> RegisterAsync(string nom, string telephone, string marche, decimal? budget, string pin);
    Task LogoutAsync();

    Task<(bool Ok, string Message, string? CodeDeTest)> ForgotAsync(string identifiant);
    Task<AuthResult> ResetAsync(string identifiant, string code, string nouveauPin);
}
