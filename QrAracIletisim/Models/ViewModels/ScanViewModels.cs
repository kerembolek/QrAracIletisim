namespace QrAracIletisim.Models.ViewModels;

public record ScanViewModel(string Token, string MaskedPlate, StickerStatus Status);

public record ContactRow(int Id, DateTime CreatedAt, ContactType Type, string Text, ContactResult Result, string Visitor, bool IsBlocked);

public record BlockedRow(int Id, string Visitor, DateTime CreatedAt);

public class StickerDetailsViewModel
{
    public int Id { get; set; }
    public string Vehicle { get; set; } = "";
    public StickerStatus Status { get; set; }
    public int HourlyMessageLimit { get; set; }
    public List<ContactRow> Contacts { get; set; } = [];
    public List<BlockedRow> Blocked { get; set; } = [];
}
