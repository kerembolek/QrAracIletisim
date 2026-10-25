using System.ComponentModel.DataAnnotations;

namespace QrAracIletisim.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Telefon numarası veya e-posta girin.")]
    [Display(Name = "Telefon numarası veya e-posta")]
    public string Login { get; set; } = "";

    [Required(ErrorMessage = "Şifre girin.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = "";

    [Display(Name = "Beni hatırla")]
    public bool RememberMe { get; set; }
}
