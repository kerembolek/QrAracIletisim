using Microsoft.AspNetCore.Identity;

namespace QrAracIletisim.Models;

// Vehicle owners sign in with their phone number as the user name; admins sign in with their e-mail
public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }

    // Owners choose how they hear about messages; SMS unless they switch to WhatsApp
    public bool NotifyViaWhatsApp { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
