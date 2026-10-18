namespace KiloKilo.Services;

/// <summary>Authentification simulée. Sera remplacée par l'appel au backend (JWT / PIN) au prochain incrément.</summary>
public class MockAuthService : IAuthService
{
    private readonly IKiloKiloService _data;
    public MockAuthService(IKiloKiloService data) => _data = data;

    public bool IsAuthenticated { get; private set; }

    public async Task<bool> LoginAsync(string identifiant, string pin)
    {
        await Task.Delay(350);
        IsAuthenticated = !string.IsNullOrWhiteSpace(identifiant) && pin.Length == 4;
        return IsAuthenticated;
    }

    public async Task<bool> RegisterAsync(string nom, string telephone, string marche, decimal? budget, string pin)
    {
        await Task.Delay(450);
        _data.Profil.NomComplet = nom;
        _data.Profil.Telephone = telephone;
        if (budget is > 0) _data.Profil.BudgetMensuel = budget.Value;
        IsAuthenticated = true;
        return true;
    }

    public Task LogoutAsync()
    {
        IsAuthenticated = false;
        return Task.CompletedTask;
    }
}
