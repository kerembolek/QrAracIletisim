using System.Security.Cryptography;
using System.Text;

namespace QrAracIletisim.Services;

// Keyed one-way hash for SMS codes and visitor IPs: without the key, a leaked database cannot be reversed by trying every value
public class SecureHash(IConfiguration configuration)
{
    private readonly byte[] key = Encoding.UTF8.GetBytes(configuration["Security:HashKey"]
        ?? throw new InvalidOperationException("Security:HashKey ayarı tanımlı değil."));

    public string Compute(string value) =>
        Convert.ToHexStringLower(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(value)));
}
