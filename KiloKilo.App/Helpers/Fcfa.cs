using System.Globalization;

namespace KiloKilo.Helpers;

public static class Fcfa
{
    private static readonly CultureInfo Fr = new("fr-FR");

    /// <summary>86500 → "86 500 FCFA" (espaces insécables)</summary>
    public static string Format(decimal value) => Number(value) + "\u00A0FCFA";

    public static string Number(decimal value)
        => value.ToString("N0", Fr).Replace('\u202F', '\u00A0');

    /// <summary>18000 → "18k"</summary>
    public static string Short(decimal value)
        => (value / 1000m).ToString("0.#", CultureInfo.InvariantCulture) + "k";

    public static string Percent(double value, int decimals = 0)
        => value.ToString(decimals == 0 ? "0" : "0." + new string('0', decimals), Fr) + "\u00A0%";

    public static bool TryParse(string? text, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var clean = new string(text.Where(c => char.IsDigit(c) || c == ',' || c == '.').ToArray())
            .Replace(',', '.');
        return decimal.TryParse(clean, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }
}
