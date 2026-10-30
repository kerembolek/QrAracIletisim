using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QrAracIletisim.Data;
using QrAracIletisim.Models;
using QrAracIletisim.Models.ViewModels;

namespace QrAracIletisim.Controllers;

[Authorize(Roles = Roles.Admin)]
public class DashboardController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        var byStatus = await context.Stickers
            .GroupBy(s => s.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        var since = DateTime.UtcNow.AddHours(-24);
        return View(new DashboardViewModel
        {
            Unclaimed = byStatus.GetValueOrDefault(StickerStatus.Unclaimed),
            Active = byStatus.GetValueOrDefault(StickerStatus.Active),
            Paused = byStatus.GetValueOrDefault(StickerStatus.Paused),
            Revoked = byStatus.GetValueOrDefault(StickerStatus.Revoked),
            Owners = await context.Stickers.Where(s => s.OwnerId != null).Select(s => s.OwnerId).Distinct().CountAsync(),
            Scans = await context.ScanEvents.CountAsync(),
            ScansLast24Hours = await context.ScanEvents.CountAsync(s => s.ScannedAt >= since),
            MessagesDelivered = await context.ContactRequests.CountAsync(c => c.Type == ContactType.Message && c.Result == ContactResult.Delivered),
            CallRequests = await context.ContactRequests.CountAsync(c => c.Type == ContactType.CallRequest && c.Result == ContactResult.Delivered),
            Refused = await context.ContactRequests.CountAsync(c => c.Result != ContactResult.Delivered)
        });
    }
}
