namespace KiloKilo.Shared;

// ============ Authentification ============
public record RegisterRequest(string NomComplet, string Telephone, string MarcheReference, decimal? BudgetMensuel, string Pin);
public record LoginRequest(string Identifiant, string Pin);
public record RefreshRequest(string RefreshToken);
public record ForgotRequest(string Identifiant);
public record ForgotResponse(string Message, string? CodeDeTest);
public record ResetRequest(string Identifiant, string Code, string NouveauPin);

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    /// <summary>Expiration du jeton d'accès (millisecondes Unix, UTC).</summary>
    public long ExpiresAt { get; set; }
    public ProfilDto Profil { get; set; } = new();
}

// ============ Profil & marchés ============
public class ProfilDto
{
    public Guid Id { get; set; }
    public string NomComplet { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Localisation { get; set; } = string.Empty;
    public decimal BudgetMensuel { get; set; }
    public int SeuilAlertePct { get; set; } = 85;
    public DateOnly MembreDepuis { get; set; }
}

public class MarcheDto
{
    public Guid Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool EstPrincipal { get; set; }
}

// ============ Courses ============
public class ArticleDto
{
    public string Nom { get; set; } = string.Empty;
    public string Quantite { get; set; } = string.Empty;
    public decimal Prix { get; set; }
    /// <summary>Calculé par le serveur, ex. « 700 F/kg ».</summary>
    public string? PrixUnitaire { get; set; }
    /// <summary>Calculé par le serveur, ex. « -6 % vs dernier relevé ».</summary>
    public string? Tendance { get; set; }
}

public class CourseDto
{
    public Guid Id { get; set; }
    public string Marche { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string ModePaiement { get; set; } = "Espèces";
    public decimal Economie { get; set; }
    public string? NoteEconomie { get; set; }
    public List<ArticleDto> Articles { get; set; } = new();
    /// <summary>Dernière modification côté serveur (millisecondes Unix, UTC).</summary>
    public long UpdatedAt { get; set; }
    /// <summary>Marqueur de suppression (utilisé uniquement par la synchronisation incrémentale).</summary>
    public bool Supprime { get; set; }

    public decimal Total => Articles.Sum(a => a.Prix);
}

public class SyncCoursesResponse
{
    public long ServerTime { get; set; }
    public List<CourseDto> Courses { get; set; } = new();
}

// ============ Prix par produit ============
public class PointPrixDto
{
    public DateOnly Date { get; set; }
    public decimal Prix { get; set; }
    public string Marche { get; set; } = string.Empty;
    public string Quantite { get; set; } = string.Empty;
}

public class ProduitDto
{
    public string Nom { get; set; } = string.Empty;
    public string Unite { get; set; } = string.Empty;
    public List<PointPrixDto> Points { get; set; } = new();
}

// ============ Statistiques ============
public class BarreDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Montant { get; set; }
    public bool EstPic { get; set; }
}

public class CategorieDto
{
    public string Nom { get; set; } = string.Empty;
    public decimal Montant { get; set; }
    public double Pourcentage { get; set; }
}

public class MarcheStatDto
{
    public string Nom { get; set; } = string.Empty;
    public int Courses { get; set; }
    public decimal Montant { get; set; }
    public double Pourcentage { get; set; }
}

public class StatsDto
{
    public string Periode { get; set; } = "Mois";
    public decimal Depense { get; set; }
    public decimal Precedent { get; set; }
    public double VariationPanierPct { get; set; }
    public decimal PanierMoyen { get; set; }
    public string LibelleComparaison { get; set; } = string.Empty;
    public string TitreRythme { get; set; } = string.Empty;
    public List<BarreDto> Barres { get; set; } = new();
    public List<CategorieDto> Categories { get; set; } = new();
    public List<MarcheStatDto> Marches { get; set; } = new();
    public string Apercu { get; set; } = string.Empty;
}
