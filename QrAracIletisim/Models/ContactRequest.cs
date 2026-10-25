using System.ComponentModel.DataAnnotations;

namespace QrAracIletisim.Models;

public enum ContactType
{
    [Display(Name = "Mesaj")]
    Message,
    [Display(Name = "Arama isteği")]
    CallRequest
}

public enum ContactResult
{
    [Display(Name = "İletildi")]
    Delivered,
    [Display(Name = "Sınıra takıldı")]
    RateLimited,
    [Display(Name = "Engellendi")]
    Blocked,
    [Display(Name = "Reddedildi")]
    Refused
}

// Every attempt from the public page is recorded, including refused ones, so limits and stats can count them
public class ContactRequest
{
    public int Id { get; set; }

    public int StickerId { get; set; }
    public Sticker? Sticker { get; set; }

    public ContactType Type { get; set; }

    // Key of a fixed preset message (see PresetMessages); visitors cannot type free text
    [StringLength(30)]
    public string? PresetKey { get; set; }

    [StringLength(64)]
    public string IpHash { get; set; } = "";

    public ContactResult Result { get; set; }

    // Virtual number handed out by the call masking provider for a call request
    [StringLength(20)]
    public string? MaskedNumber { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
