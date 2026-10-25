using System.ComponentModel.DataAnnotations;

namespace QrAracIletisim.Models;

public class Vehicle
{
    public int Id { get; set; }

    public string OwnerId { get; set; } = "";
    public ApplicationUser? Owner { get; set; }

    // Stored normalized (upper case, no spaces, e.g. 34ABC123); screens show it masked
    [Required]
    [StringLength(10)]
    [Display(Name = "Plaka")]
    public string Plate { get; set; } = "";

    [StringLength(50)]
    [Display(Name = "Araç adı")]
    public string? Nickname { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
