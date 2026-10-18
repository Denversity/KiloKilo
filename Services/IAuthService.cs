namespace KiloKilo.Services;

public interface IAuthService
{
    bool IsAuthenticated { get; }
    Task<bool> LoginAsync(string identifiant, string pin);
    Task<bool> RegisterAsync(string nom, string telephone, string marche, decimal? budget, string pin);
    Task LogoutAsync();
}
