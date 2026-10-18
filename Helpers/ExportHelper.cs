using System.Text;
using KiloKilo.Models;

namespace KiloKilo.Helpers;

/// <summary>Export CSV « léger » des relevés (fichier ouvert et portable).</summary>
public static class ExportHelper
{
    public static async Task ExportCsvAsync(IEnumerable<Course> courses)
    {
        var sb = new StringBuilder();
        sb.AppendLine("date;marche;article;quantite;prix_fcfa;paiement");
        foreach (var c in courses.OrderBy(c => c.Date))
            foreach (var a in c.Articles)
                sb.AppendLine($"{c.Date:yyyy-MM-dd};{Esc(c.Marche)};{Esc(a.Nom)};{Esc(a.Quantite)};{a.Prix:0};{Esc(c.ModePaiement)}");

        var path = Path.Combine(FileSystem.CacheDirectory, "kilokilo_releves.csv");
        await File.WriteAllTextAsync(path, sb.ToString(), new UTF8Encoding(true));
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Relevés KiloKilo",
            File = new ShareFile(path)
        });
    }

    private static string Esc(string s) => s.Replace(";", ",").Replace("\n", " ");
}
