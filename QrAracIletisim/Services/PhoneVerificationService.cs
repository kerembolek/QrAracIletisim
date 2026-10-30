using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using QrAracIletisim.Data;
using QrAracIletisim.Models;
using QrAracIletisim.Services.Providers;

namespace QrAracIletisim.Services;

public class PhoneVerificationService(ApplicationDbContext context, ISmsSender smsSender, SecureHash secureHash)
{
    // SMS costs money, so one number gets only a few codes in a short window
    private const int MaxCodesPerWindow = 3;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(10);

    // Returns false when the number asked for too many codes. When the number already has an account, the owner
    // gets a notice instead of a code, and the caller shows the same screen, so the form cannot reveal who is registered
    public async Task<bool> SendCodeAsync(string phoneNumber, VerificationPurpose purpose, string? userId, bool numberTaken)
    {
        var since = DateTime.UtcNow - Window;
        var recent = await context.PhoneVerifications.CountAsync(p => p.PhoneNumber == phoneNumber && p.CreatedAt >= since);
        if (recent >= MaxCodesPerWindow)
            return false;

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        context.PhoneVerifications.Add(new PhoneVerification
        {
            PhoneNumber = phoneNumber,
            CodeHash = HashCode(phoneNumber, code),
            Purpose = purpose,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow + PhoneVerification.Lifetime
        });
        await context.SaveChangesAsync();

        await smsSender.SendAsync(phoneNumber, numberTaken
            ? "Bu numarayla kayıtlı bir QR Araç İletişim hesabı zaten var. İşlemi siz yapmadıysanız bu mesajı dikkate almayın."
            : $"QR Araç İletişim doğrulama kodunuz: {code}. Kod {PhoneVerification.Lifetime.TotalMinutes:0} dakika geçerlidir.");
        return true;
    }

    // Only the newest code counts, and wrong tries use up its attempts
    public async Task<bool> VerifyAsync(string phoneNumber, string code, VerificationPurpose purpose, string? userId)
    {
        var verification = await context.PhoneVerifications
            .Where(p => p.PhoneNumber == phoneNumber && p.Purpose == purpose && p.ConsumedAt == null)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        if (verification is null || verification.UserId != userId
            || verification.ExpiresAt < DateTime.UtcNow || verification.Attempts >= PhoneVerification.MaxAttempts)
            return false;

        var matches = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(verification.CodeHash),
            Encoding.UTF8.GetBytes(HashCode(phoneNumber, code.Trim())));

        if (matches)
            verification.ConsumedAt = DateTime.UtcNow;
        else
            verification.Attempts++;

        await context.SaveChangesAsync();
        return matches;
    }

    private string HashCode(string phoneNumber, string code) => secureHash.Compute($"{phoneNumber}:{code}");
}
