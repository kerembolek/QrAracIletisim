using Microsoft.AspNetCore.Identity;

namespace QrAracIletisim.Models;

// Vehicle owners sign in with their phone number as the user name; admins sign in with their e-mail
public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
