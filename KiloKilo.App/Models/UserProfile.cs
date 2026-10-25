namespace KiloKilo.Models;

public class UserProfile
{
    public string NomComplet { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Localisation { get; set; } = string.Empty;
    public string MembreDepuis { get; set; } = string.Empty;
    public decimal BudgetMensuel { get; set; }
    public int SeuilAlertePct { get; set; } = 85;
    public string Prenom => NomComplet.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? NomComplet;
}
