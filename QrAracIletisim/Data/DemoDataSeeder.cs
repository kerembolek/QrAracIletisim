using Microsoft.AspNetCore.Identity;
using QrAracIletisim.Models;

namespace QrAracIletisim.Data;

// Development-only demo data; safe to run on every start because each record is matched on its e-mail or phone first
public static class DemoDataSeeder
{
    public const string AdminEmail = "admin@qrarac.demo";
    public const string DemoPassword = "Demo.12345";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        if (await userManager.FindByEmailAsync(AdminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                EmailConfirmed = true,
                DisplayName = "Demo Yönetici"
            };
            var result = await userManager.CreateAsync(admin, DemoPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, Roles.Admin);
        }
    }
}
