using System.ComponentModel.DataAnnotations;

namespace QrAracIletisim.Models;

// One opening of the public sticker page, for statistics
public class ScanEvent
{
    public int Id { get; set; }

    public int StickerId { get; set; }
    public Sticker? Sticker { get; set; }

    // Salted SHA-256 of the visitor's IP; the address itself is never stored
    [StringLength(64)]
    public string IpHash { get; set; } = "";

    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
}
