using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace QrAracIletisim.Models;

public static class EnumExtensions
{
    // The Turkish label from an enum value's [Display] attribute
    public static string Label(this Enum value) =>
        value.GetType().GetField(value.ToString())?.GetCustomAttribute<DisplayAttribute>()?.Name ?? value.ToString();

    public static string BadgeClass(this StickerStatus status) => status switch
    {
        StickerStatus.Active => "text-bg-success",
        StickerStatus.Paused => "text-bg-warning",
        StickerStatus.Revoked => "text-bg-danger",
        _ => "text-bg-secondary"
    };
}
