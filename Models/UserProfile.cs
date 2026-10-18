namespace KiloKilo.Models;

public class UserProfile
{
    public string NomComplet { get; set; } = "Amina Nguemo";
    public string Telephone { get; set; } = "+237 690 12 34 56";
    public string Localisation { get; set; } = "Yaoundé, Cameroun";
    public string MembreDepuis { get; set; } = "Membre active depuis août 2024";
    public decimal EconomiesTrimestre { get; set; } = 34_500m;
    public decimal BudgetMensuel { get; set; } = 150_000m;
    public int SeuilAlertePct { get; set; } = 85;
    public string Prenom => NomComplet.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? NomComplet;
}
