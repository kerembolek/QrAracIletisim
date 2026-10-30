using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QrAracIletisim.Data;
using QrAracIletisim.Models;
using QrAracIletisim.Models.ViewModels;
using QrAracIletisim.Services;

namespace QrAracIletisim.Controllers;

[Authorize(Roles = Roles.Admin)]
public class UsersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var adminIds = (await userManager.GetUsersInRoleAsync(Roles.Admin)).Select(u => u.Id).ToHashSet();
        var stickerCounts = await context.Stickers
            .Where(s => s.OwnerId != null)
            .GroupBy(s => s.OwnerId!)
            .Select(g => new { OwnerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.OwnerId, x => x.Count);

        var users = await context.Users.AsNoTracking().OrderByDescending(u => u.CreatedAt).ToListAsync();
        var rows = users.Select(u => new UserRow(
            u.DisplayName ?? "-",
            // Even admins see owners' numbers masked
            u.PhoneNumber is null ? u.UserName ?? "-" : PhoneNumbers.Mask(u.PhoneNumber),
            adminIds.Contains(u.Id) ? "Yönetici" : "Araç sahibi",
            stickerCounts.GetValueOrDefault(u.Id),
            u.CreatedAt)).ToList();

        return View(rows);
    }
}
