using System.Text.RegularExpressions;

namespace QrAracIletisim.Services;

// Turkish mobile numbers in one stored shape: +905XXXXXXXXX
public static partial class PhoneNumbers
{
    // Accepts "0532 123 45 67", "532-123-4567", "+90 532 123 45 67" and similar; returns null when it is not a mobile number
    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var digits = NonDigits().Replace(input, "");
        if (digits.StartsWith("90") && digits.Length == 12)
            digits = digits[2..];
        else if (digits.StartsWith('0') && digits.Length == 11)
            digits = digits[1..];

        return digits.Length == 10 && digits[0] == '5' ? "+90" + digits : null;
    }

    // "+905321234567" -> "0532 *** ** 67", so screens never show a full number
    public static string Mask(string? phone)
    {
        if (phone is null || phone.Length != 13)
            return "-";

        return $"0{phone[3..6]} *** ** {phone[^2..]}";
    }

    [GeneratedRegex(@"\D")]
    private static partial Regex NonDigits();
}
