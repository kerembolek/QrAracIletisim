using System.ComponentModel.DataAnnotations;

namespace QrAracIletisim.Models;

public enum StickerStatus
{
    [Display(Name = "Sahipsiz")]
    Unclaimed,
    [Display(Name = "Aktif")]
    Active,
    [Display(Name = "Duraklatıldı")]
    Paused,
    [Display(Name = "İptal edildi")]
    Revoked
}

public class Sticker
{
    public const int DefaultHourlyMessageLimit = 5;

    public int Id { get; set; }

    // Random, unguessable value printed in the QR code (/s/{token}); the numeric Id never leaves the server
    [Required]
    [StringLength(32)]
    public string Token { get; set; } = "";

    public int BatchId { get; set; }
    public StickerBatch? Batch { get; set; }

    public StickerStatus Status { get; set; } = StickerStatus.Unclaimed;

    public string? OwnerId { get; set; }
    public ApplicationUser? Owner { get; set; }

    public int? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    [Range(1, 30)]
    public int HourlyMessageLimit { get; set; } = DefaultHourlyMessageLimit;

    public DateTime? ClaimedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
