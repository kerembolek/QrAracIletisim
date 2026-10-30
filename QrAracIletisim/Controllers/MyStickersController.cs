using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QrAracIletisim.Data;
using QrAracIletisim.Models;
using QrAracIletisim.Models.ViewModels;
using QrAracIletisim.Services;

namespace QrAracIletisim.Controllers;

[Authorize(Roles = Roles.Owner)]
public class MyStickersController(ApplicationDbContext context) : Controller
{
    private string OwnerId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index()
    {
        var since = DateTime.UtcNow.AddDays(-7);
        var ownerId = OwnerId;
        var stickers = await context.Stickers.AsNoTracking()
            .Where(s => s.OwnerId == ownerId)
            .OrderByDescending(s => s.ClaimedAt)
            .Select(s => new
            {
                s.Id,
                Plate = s.Vehicle!.Plate,
                s.Vehicle.Nickname,
                s.Status,
                s.ClaimedAt,
                Messages = context.ContactRequests.Count(c => c.StickerId == s.Id && c.Result == ContactResult.Delivered && c.CreatedAt >= since)
            })
            .ToListAsync();

        return View(stickers
            .Select(s => new OwnerStickerRow(s.Id, Vehicle(s.Plate, s.Nickname), s.Status, s.ClaimedAt, s.Messages))
            .ToList());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Pause(int id) =>
        ChangeStatusAsync(id, [StickerStatus.Active], StickerStatus.Paused, "Etiket duraklatıldı; okutulduğunda mesaj kabul etmez.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Resume(int id) =>
        ChangeStatusAsync(id, [StickerStatus.Paused], StickerStatus.Active, "Etiket yeniden aktif.");

    // Permanent: a lost or stolen sticker must not come back to life
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Revoke(int id) =>
        ChangeStatusAsync(id, [StickerStatus.Active, StickerStatus.Paused], StickerStatus.Revoked, "Etiket iptal edildi. Bu işlem geri alınamaz.");

    private async Task<IActionResult> ChangeStatusAsync(int id, StickerStatus[] from, StickerStatus to, string message)
    {
        var ownerId = OwnerId;
        var sticker = await context.Stickers.FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == ownerId);
        if (sticker is null)
            return NotFound();

        if (from.Contains(sticker.Status))
        {
            sticker.Status = to;
            await context.SaveChangesAsync();
            TempData["Success"] = message;
        }
        else
        {
            TempData["Error"] = "Bu etiketin durumu bu işleme uygun değil.";
        }
        return RedirectToAction(nameof(Index));
    }

    private static string Vehicle(string plate, string? nickname) =>
        string.IsNullOrEmpty(nickname) ? Plates.Mask(plate) : $"{nickname} ({Plates.Mask(plate)})";
}
