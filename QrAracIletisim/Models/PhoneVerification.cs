using System.ComponentModel.DataAnnotations;

namespace QrAracIletisim.Models;

public enum VerificationPurpose
{
    Register,
    ChangePhone
}

// A one-time SMS code; only its hash is stored, and it expires after a few minutes or a few wrong tries
public class PhoneVerification
{
    public const int MaxAttempts = 5;
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    public int Id { get; set; }

    [Required]
    [StringLength(13)]
    public string PhoneNumber { get; set; } = "";

    [Required]
    [StringLength(64)]
    public string CodeHash { get; set; } = "";

    public VerificationPurpose Purpose { get; set; }

    // Set for a phone change, where the account already exists
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public int Attempts { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? ConsumedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
