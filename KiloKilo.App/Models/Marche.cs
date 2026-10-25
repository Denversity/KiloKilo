namespace KiloKilo.Models;

public class Marche
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nom { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool EstPrincipal { get; set; }
}
