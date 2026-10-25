namespace KiloKilo.Api.Data;

public class UserEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string NomComplet { get; set; } = string.Empty;
    /// <summary>Numéro de téléphone normalisé (chiffres, éventuel « + » initial).</summary>
    public string Identifiant { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PinHash { get; set; } = string.Empty;
    public string Localisation { get; set; } = string.Empty;
    public decimal BudgetMensuel { get; set; }
    public int SeuilAlertePct { get; set; } = 85;
    public DateOnly CreeLe { get; set; }

    // Protection contre la devinette du PIN (4 chiffres)
    public int EchecsConnexion { get; set; }
    public long VerrouJusqua { get; set; }

    // Réinitialisation du PIN
    public string? ResetCodeHash { get; set; }
    public long ResetExpireAt { get; set; }
}

public class MarcheEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool EstPrincipal { get; set; }
}

public class CourseEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Marche { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string ModePaiement { get; set; } = "Espèces";
    public decimal Economie { get; set; }
    public string? NoteEconomie { get; set; }
    public long UpdatedAt { get; set; }
    public bool Supprime { get; set; }
    public List<ArticleEntity> Articles { get; set; } = new();
}

public class ArticleEntity
{
    public int Id { get; set; }
    public Guid CourseId { get; set; }
    public CourseEntity Course { get; set; } = null!;
    public string Nom { get; set; } = string.Empty;
    public string NomNormalise { get; set; } = string.Empty;
    public string Quantite { get; set; } = string.Empty;
    public decimal Prix { get; set; }
    public decimal PrixParUnite { get; set; }
    public string? PrixUnitaire { get; set; }
    public string? Tendance { get; set; }
}

public class RefreshTokenEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public long ExpiresAt { get; set; }
    public bool Revoque { get; set; }
}
