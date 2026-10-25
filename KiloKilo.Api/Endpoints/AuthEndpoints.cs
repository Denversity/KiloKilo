using System.Security.Cryptography;
using KiloKilo.Api.Data;
using KiloKilo.Api.Services;
using KiloKilo.Shared;
using Microsoft.EntityFrameworkCore;

namespace KiloKilo.Api.Endpoints;

public static class AuthEndpoints
{
    private const int MaxEchecs = 5;
    private static readonly TimeSpan Verrouillage = TimeSpan.FromMinutes(15);

    public static void MapAuth(this WebApplication app)
    {
        var g = app.MapGroup("/api/auth").WithTags("Authentification");

        g.MapPost("/register", async (RegisterRequest r, KiloDb db, TokenService tokens) =>
        {
            var nom = (r.NomComplet ?? string.Empty).Trim();
            var ident = Identifiants.Normaliser(r.Telephone);
            var chiffres = ident.Count(char.IsDigit);

            if (nom.Length < 2 || nom.Length > 120) return Reponses.Erreur(400, "Le nom complet est requis (2 à 120 caractères).");
            if (ident.Contains('@') || chiffres < 8 || chiffres > 15) return Reponses.Erreur(400, "Numéro de téléphone invalide.");
            if (!PinValide(r.Pin)) return Reponses.Erreur(400, "Le code secret doit comporter exactement 4 chiffres.");
            if (r.BudgetMensuel is < 0 or > 1_000_000_000) return Reponses.Erreur(400, "Budget mensuel invalide.");
            if (await db.Users.AnyAsync(u => u.Identifiant == ident))
                return Reponses.Erreur(409, "Un compte existe déjà avec ce numéro. Connectez-vous.");

            var user = new UserEntity
            {
                NomComplet = nom,
                Identifiant = ident,
                PinHash = PinHasher.Hash(r.Pin),
                BudgetMensuel = r.BudgetMensuel ?? 0,
                CreeLe = DateOnly.FromDateTime(DateTime.UtcNow)
            };
            db.Users.Add(user);

            if (!string.IsNullOrWhiteSpace(r.MarcheReference))
                db.Marches.Add(new MarcheEntity
                {
                    Id = Guid.NewGuid(), UserId = user.Id, Nom = r.MarcheReference.Trim(),
                    Description = "Marché de référence", EstPrincipal = true
                });

            await db.SaveChangesAsync();
            return Results.Ok(await EmettreAsync(db, tokens, user));
        });

        g.MapPost("/login", async (LoginRequest r, KiloDb db, TokenService tokens) =>
        {
            var ident = Identifiants.Normaliser(r.Identifiant);
            var user = await db.Users.FirstOrDefaultAsync(u => u.Identifiant == ident || (u.Email != null && u.Email == ident));
            const string generique = "Identifiant ou code secret incorrect.";
            if (user is null || !PinValide(r.Pin)) return Reponses.Erreur(401, generique, "Connexion refusée");

            var now = Clock.NowMs();
            if (user.VerrouJusqua > now)
            {
                var minutes = (int)Math.Ceiling((user.VerrouJusqua - now) / 60000.0);
                return Reponses.Erreur(429, $"Trop de tentatives. Réessayez dans {minutes} min.", "Compte verrouillé");
            }

            if (!PinHasher.Verify(r.Pin, user.PinHash))
            {
                user.EchecsConnexion++;
                if (user.EchecsConnexion >= MaxEchecs)
                {
                    user.VerrouJusqua = now + (long)Verrouillage.TotalMilliseconds;
                    user.EchecsConnexion = 0;
                }
                await db.SaveChangesAsync();
                return Reponses.Erreur(401, generique, "Connexion refusée");
            }

            user.EchecsConnexion = 0;
            user.VerrouJusqua = 0;
            await db.SaveChangesAsync();
            return Results.Ok(await EmettreAsync(db, tokens, user));
        });

        g.MapPost("/refresh", async (RefreshRequest r, KiloDb db, TokenService tokens) =>
        {
            if (string.IsNullOrWhiteSpace(r.RefreshToken)) return Reponses.Erreur(400, "Jeton manquant.");
            var hash = TokenService.Hash(r.RefreshToken);
            var rt = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
            if (rt is null || rt.Revoque || rt.ExpiresAt < Clock.NowMs())
                return Reponses.Erreur(401, "Session expirée. Reconnectez-vous.", "Session invalide");

            var user = await db.Users.FindAsync(rt.UserId);
            if (user is null) return Reponses.Erreur(401, "Session invalide.");

            rt.Revoque = true; // rotation : un jeton de rafraîchissement ne sert qu'une fois
            await db.SaveChangesAsync();
            return Results.Ok(await EmettreAsync(db, tokens, user));
        });

        g.MapPost("/logout", async (RefreshRequest r, KiloDb db) =>
        {
            if (!string.IsNullOrWhiteSpace(r.RefreshToken))
            {
                var hash = TokenService.Hash(r.RefreshToken);
                var rt = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
                if (rt is not null) { rt.Revoque = true; await db.SaveChangesAsync(); }
            }
            return Results.NoContent();
        });

        g.MapPost("/forgot", async (ForgotRequest r, KiloDb db, INotificationSender sender, IWebHostEnvironment env) =>
        {
            const string message = "Si un compte correspond à cet identifiant, un code de réinitialisation vient d'être envoyé.";
            var ident = Identifiants.Normaliser(r.Identifiant);
            var user = await db.Users.FirstOrDefaultAsync(u => u.Identifiant == ident || (u.Email != null && u.Email == ident));
            if (user is null) return Results.Ok(new ForgotResponse(message, null));

            var code = RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString();
            user.ResetCodeHash = TokenService.Hash(user.Id + code);
            user.ResetExpireAt = Clock.NowMs() + 10 * 60_000;
            await db.SaveChangesAsync();

            await sender.EnvoyerAsync(user.Identifiant, $"KiloKilo : votre code de réinitialisation est {code} (valable 10 min).");
            return Results.Ok(new ForgotResponse(message, env.IsDevelopment() ? code : null));
        });

        g.MapPost("/reset", async (ResetRequest r, KiloDb db) =>
        {
            if (!PinValide(r.NouveauPin)) return Reponses.Erreur(400, "Le nouveau code secret doit comporter exactement 4 chiffres.");
            var ident = Identifiants.Normaliser(r.Identifiant);
            var user = await db.Users.FirstOrDefaultAsync(u => u.Identifiant == ident || (u.Email != null && u.Email == ident));
            const string invalide = "Code invalide ou expiré.";
            if (user is null || user.ResetCodeHash is null || user.ResetExpireAt < Clock.NowMs())
                return Reponses.Erreur(400, invalide);

            var attendu = TokenService.Hash(user.Id + (r.Code ?? string.Empty).Trim());
            if (!CryptographicOperations.FixedTimeEquals(System.Text.Encoding.UTF8.GetBytes(attendu), System.Text.Encoding.UTF8.GetBytes(user.ResetCodeHash)))
                return Reponses.Erreur(400, invalide);

            user.PinHash = PinHasher.Hash(r.NouveauPin);
            user.ResetCodeHash = null;
            user.ResetExpireAt = 0;
            user.EchecsConnexion = 0;
            user.VerrouJusqua = 0;
            foreach (var t in db.RefreshTokens.Where(t => t.UserId == user.Id && !t.Revoque)) t.Revoque = true;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static bool PinValide(string? pin) => pin is { Length: 4 } && pin.All(char.IsDigit);

    private static async Task<AuthResponse> EmettreAsync(KiloDb db, TokenService tokens, UserEntity user)
    {
        var (access, expires) = tokens.CreerAccessToken(user);
        var refresh = TokenService.NouveauRefreshToken();
        db.RefreshTokens.Add(new RefreshTokenEntity
        {
            UserId = user.Id,
            TokenHash = TokenService.Hash(refresh),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(tokens.RefreshDays).ToUnixTimeMilliseconds()
        });
        await db.SaveChangesAsync();
        return new AuthResponse { AccessToken = access, RefreshToken = refresh, ExpiresAt = expires, Profil = user.ToDto() };
    }
}
