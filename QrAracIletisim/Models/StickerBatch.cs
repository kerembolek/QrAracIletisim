using System.ComponentModel.DataAnnotations;

namespace QrAracIletisim.Models;

// A group of stickers generated together by an admin; the print sheet opens per batch
public class StickerBatch
{
    public int Id { get; set; }

    public int Count { get; set; }

    [StringLength(200)]
    [Display(Name = "Not")]
    public string? Note { get; set; }

    public string CreatedById { get; set; } = "";
    public ApplicationUser? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Sticker> Stickers { get; set; } = [];
}
