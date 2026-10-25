using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace KiloKilo.Shared;

/// <summary>Utilitaires de texte et de quantités partagés par l'API et l'application.</summary>
public static class Texte
{
    public static readonly CultureInfo Fr = new("fr-FR");

    private static readonly Regex QuantiteRx = new(@"^\s*(\d+(?:[.,]\d+)?)\s*(\p{L}+)?", RegexOptions.Compiled);

    public static string SansAccents(string s)
    {
        var d = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(d.Length);
        foreach (var c in d)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>Clé de regroupement d'un produit (sans casse ni espaces superflus).</summary>
    public static string NormaliserNom(string? nom) => (nom ?? string.Empty).Trim().ToLowerInvariant();

    /// <summary>« 3 kg » → (3, "kg") ; « 1 seau • environ 5kg » → (1, "seau") ; illisible → (1, "").</summary>
    public static (decimal Nombre, string Unite) AnalyserQuantite(string? quantite)
    {
        var m = QuantiteRx.Match(quantite ?? string.Empty);
        if (!m.Success) return (1, string.Empty);
        var ok = decimal.TryParse(m.Groups[1].Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var n);
        return (ok && n > 0 ? n : 1, m.Groups[2].Value);
    }

    public static decimal PrixParUnite(ArticleDto a)
        => Math.Round(a.Prix / AnalyserQuantite(a.Quantite).Nombre, 0);

    public static string Fcfa(decimal v) => v.ToString("N0", Fr).Replace('\u202F', '\u00A0') + "\u00A0FCFA";
}

/// <summary>Rattache un produit à l'une des familles de vivres des statistiques.</summary>
public static class Categories
{
    public const string Legumes = "Légumes & Condiments";
    public const string Fruits = "Fruits";
    public const string Feculents = "Féculents & Céréales";
    public const string Viandes = "Viandes & Poissons";
    public const string Epicerie = "Huiles & Épicerie";

    private static readonly (string Categorie, string[] Mots)[] Regles =
    {
        (Feculents, new[] { "plantain", "riz", "manioc", "igname", "pomme de terre", "macabo", "mais", "farine", "pain", "haricot", "arachide", "patate", "couscous", "de terre", "pate", "spaghetti", "bobolo", "miondo", "tapioca", "gari" }),
        (Viandes, new[] { "viande", "boeuf", "bœuf", "œuf", "volaille", "poisson", "bar ", "bar", "poulet", "porc", "crevette", "oeuf", "chevre", "mouton", "sardine", "maquereau", "capitaine", "silure", "bushmeat", "gibier", "dinde", "canard" }),
        (Fruits, new[] { "avocat", "ananas", "papaye", "citron", "mangue", "orange", "banane", "pasteque", "goyave", "mandarine", "fruit", "safou", "corossol", "pomme" }),
        (Epicerie, new[] { "huile", "sel", "sucre", "savon", "lait", "cafe", "the ", "beurre", "mayonnaise", "cube", "conserve", "tomate concentree", "epicerie", "farine de ble" }),
        (Legumes, new[] { "tomate", "oignon", "piment", "carotte", "poireau", "ail", "gingembre", "legume", "chou", "salade", "poivron", "concombre", "epinard", "aubergine", "feuille", "celeri", "persil", "courge", "gombo", "ndole", "okok", "folong", "poivre", "basilic", "condiment", "epice" }),
    };

    public static string De(string nom)
    {
        var n = Texte.SansAccents(nom).ToLowerInvariant();
        foreach (var (cat, mots) in Regles)
            if (mots.Any(n.Contains)) return cat;
        return Epicerie;
    }
}
