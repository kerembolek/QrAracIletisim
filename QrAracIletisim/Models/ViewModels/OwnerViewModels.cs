using System.ComponentModel.DataAnnotations;

namespace QrAracIletisim.Models.ViewModels;

public class RegisterPhoneViewModel
{
    public string Claim { get; set; } = "";

    [Required(ErrorMessage = "Telefon numarası girin.")]
    [Display(Name = "Cep telefonu")]
    public string Phone { get; set; } = "";
}

public class VerifyPhoneViewModel
{
    public string Claim { get; set; } = "";

    public string Phone { get; set; } = "";

    [Required(ErrorMessage = "SMS kodunu girin.")]
    [Display(Name = "SMS kodu")]
    public string Code { get; set; } = "";

    [Required(ErrorMessage = "Adınızı girin.")]
    [StringLength(60, ErrorMessage = "Ad en fazla 60 karakter olabilir.")]
    [Display(Name = "Adınız")]
    public string DisplayName { get; set; } = "";

    [Required(ErrorMessage = "Şifre belirleyin.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = "";

    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Şifreler eşleşmiyor.")]
    [Display(Name = "Şifre (tekrar)")]
    public string ConfirmPassword { get; set; } = "";
}

public class ClaimViewModel
{
    [Required(ErrorMessage = "Plaka girin.")]
    [StringLength(12, ErrorMessage = "Plaka en fazla 12 karakter olabilir.")]
    [Display(Name = "Plaka")]
    public string Plate { get; set; } = "";

    [StringLength(50, ErrorMessage = "Araç adı en fazla 50 karakter olabilir.")]
    [Display(Name = "Araç adı (isteğe bağlı)")]
    public string? Nickname { get; set; }
}

public record OwnerStickerRow(int Id, string Vehicle, StickerStatus Status, DateTime? ClaimedAt, int MessagesLast7Days);

public class ProfileViewModel
{
    public string MaskedPhone { get; set; } = "";
    public bool NotifyViaWhatsApp { get; set; }
}

public class ConfirmPhoneChangeViewModel
{
    public string NewPhone { get; set; } = "";

    [Required(ErrorMessage = "SMS kodunu girin.")]
    [Display(Name = "Yeni numaraya gelen SMS kodu")]
    public string Code { get; set; } = "";

    [Required(ErrorMessage = "Şifrenizi girin.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mevcut şifreniz")]
    public string Password { get; set; } = "";
}
