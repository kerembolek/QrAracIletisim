using System.ComponentModel.DataAnnotations;

namespace QrAracIletisim.Models.ViewModels;

public class GenerateStickersViewModel
{
    public const int MaxCount = 500;

    [Range(1, MaxCount, ErrorMessage = "Etiket adedi 1 ile 500 arasında olmalıdır.")]
    [Display(Name = "Adet")]
    public int Count { get; set; } = 20;

    [StringLength(200, ErrorMessage = "Not en fazla 200 karakter olabilir.")]
    [Display(Name = "Not")]
    public string? Note { get; set; }
}

public record BatchRow(int Id, DateTime CreatedAt, int Count, string? Note, int Claimed);

public class StickersIndexViewModel
{
    public GenerateStickersViewModel Generate { get; set; } = new();
    public List<BatchRow> Batches { get; set; } = [];
}

public record PrintableSticker(string QrSvg);

public record UserRow(string Name, string Login, string Role, int StickerCount, DateTime CreatedAt);

public class DashboardViewModel
{
    public int Unclaimed { get; set; }
    public int Active { get; set; }
    public int Paused { get; set; }
    public int Revoked { get; set; }
    public int Owners { get; set; }
    public int Scans { get; set; }
    public int ScansLast24Hours { get; set; }
    public int MessagesDelivered { get; set; }
    public int CallRequests { get; set; }
    public int Refused { get; set; }
}
