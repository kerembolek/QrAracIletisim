namespace QrAracIletisim.Models;

// The only messages a visitor can send; a fixed list keeps the page free of abuse and personal data
public static class PresetMessages
{
    public static readonly IReadOnlyList<(string Key, string Text, string Icon)> All =
    [
        ("blocking", "Aracınız yolumu kapattı.", "bi-sign-stop"),
        ("lights", "Aracınızın farları açık kaldı.", "bi-lightbulb"),
        ("window", "Aracınızın camı açık kaldı.", "bi-window"),
        ("alarm", "Aracınızın alarmı çalıyor.", "bi-bell"),
        ("damage", "Aracınıza çarpıldı veya hasar var, lütfen gelin.", "bi-exclamation-octagon"),
        ("towing", "Aracınız çekiliyor veya ceza yazılıyor.", "bi-truck")
    ];

    public static string? TextFor(string? key) =>
        All.FirstOrDefault(m => m.Key == key).Text;
}
