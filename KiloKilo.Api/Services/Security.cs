using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace KiloKilo.Api.Services;

public static class Clock
{
    public static long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}

/// <summary>Hachage du PIN : PBKDF2-SHA256, sel aléatoire, comparaison à temps constant.</summary>
public static class PinHasher
{
    private const int Iterations = 120_000;

    public static string Hash(string pin)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(pin, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string pin, string stored)
    {
        var parts = stored.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iter)) return false;
        var salt = Convert.FromBase64String(parts[1]);
        var expected = Convert.FromBase64String(parts[2]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(pin, salt, iter, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}

public class TokenService
{
    private readonly IConfiguration _cfg;
    public TokenService(IConfiguration cfg) => _cfg = cfg;

    public int RefreshDays => _cfg.GetValue("Jwt:RefreshDays", 60);

    public (string Token, long ExpiresAt) CreerAccessToken(Data.UserEntity user)
    {
        var minutes = _cfg.GetValue("Jwt:AccessMinutes", 60);
        var expires = DateTime.UtcNow.AddMinutes(minutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Jwt:Key"]!));
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", user.Id.ToString()),
                new Claim("name", user.NomComplet)
            }),
            Issuer = _cfg["Jwt:Issuer"],
            Audience = _cfg["Jwt:Audience"],
            Expires = expires,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };
        return (new JsonWebTokenHandler().CreateToken(descriptor), new DateTimeOffset(expires).ToUnixTimeMilliseconds());
    }

    public static string NouveauRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
        .Replace('+', '-').Replace('/', '_').TrimEnd('=');

    public static string Hash(string valeur) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(valeur)));
}

public static class UserExtensions
{
    public static Guid UserId(this ClaimsPrincipal p)
    {
        var v = p.FindFirstValue("sub") ?? p.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(v, out var id) ? id : Guid.Empty;
    }
}

public interface INotificationSender
{
    Task EnvoyerAsync(string destinataire, string message);
}

/// <summary>Remplace l'envoi SMS / e-mail tant qu'aucun fournisseur n'est branché : le message est journalisé.</summary>
public class ConsoleNotificationSender : INotificationSender
{
    private readonly ILogger<ConsoleNotificationSender> _log;
    public ConsoleNotificationSender(ILogger<ConsoleNotificationSender> log) => _log = log;

    public Task EnvoyerAsync(string destinataire, string message)
    {
        _log.LogWarning("[NOTIFICATION → {Dest}] {Message}", destinataire, message);
        return Task.CompletedTask;
    }
}
