using System.ComponentModel.DataAnnotations;

namespace QrAracIletisim.Models;

// An owner's block on one visitor (by IP hash) for one sticker
public class BlockedVisitor
{
    public int Id { get; set; }

    public int StickerId { get; set; }
    public Sticker? Sticker { get; set; }

    [StringLength(64)]
    public string IpHash { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
