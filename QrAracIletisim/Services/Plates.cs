using System.Text.RegularExpressions;

namespace QrAracIletisim.Services;

// Turkish plates: 2-digit province code, 1-3 letters, 2-4 digits (e.g. 34 ABC 123)
public static partial class Plates
{
    // Returns the plate upper case without spaces, or null when it does not look like a Turkish plate
    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var plate = input.Replace(" ", "").Replace("-", "").ToUpperInvariant();
        return PlatePattern().IsMatch(plate) ? plate : null;
    }

    // "34ABC123" -> "34 *** 23": enough for the owner to recognise, useless to a stranger
    public static string Mask(string? plate)
    {
        if (string.IsNullOrEmpty(plate) || plate.Length < 4)
            return "-";

        return $"{plate[..2]} *** {plate[^2..]}";
    }

    [GeneratedRegex(@"^(0[1-9]|[1-7][0-9]|8[01])[A-Z]{1,3}[0-9]{2,4}$")]
    private static partial Regex PlatePattern();
}
