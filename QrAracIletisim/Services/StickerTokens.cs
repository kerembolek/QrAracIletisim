using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace QrAracIletisim.Services;

public static class StickerTokens
{
    // 128 random bits as 22 URL-safe characters: too many possibilities to guess or to walk through one by one
    public static string Generate() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(16));

    // The address printed in the QR code. Stickers outlive this machine, so a deployed site sets PublicBaseUrl
    public static string PublicUrl(HttpRequest request, IConfiguration configuration, string token)
    {
        var baseUrl = configuration["PublicBaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = $"{request.Scheme}://{request.Host}";

        return $"{baseUrl.TrimEnd('/')}/s/{token}";
    }
}
