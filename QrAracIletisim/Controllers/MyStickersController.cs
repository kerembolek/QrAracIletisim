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
    private const int HistorySize = 50;

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

    public async Task<IActionResult> Details(int id)
    {
        var ownerId = OwnerId;
        var sticker = await context.Stickers.AsNoTracking().Include(s => s.Vehicle)
            .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == ownerId);
        if (sticker is null)
            return NotFound();

        var blocked = await context.BlockedVisitors.AsNoTracking()
            .Where(b => b.StickerId == id)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
        var blockedHashes = blocked.Select(b => b.IpHash).ToHashSet();

        var contacts = await context.ContactRequests.AsNoTracking()
            .Where(c => c.StickerId == id)
            .OrderByDescending(c => c.CreatedAt)
            .Take(HistorySize)
            .ToListAsync();

        return View(new StickerDetailsViewModel
        {
            Id = sticker.Id,
            Vehicle = Vehicle(sticker.Vehicle!.Plate, sticker.Vehicle.Nickname),
            Status = sticker.Status,
            HourlyMessageLimit = sticker.HourlyMessageLimit,
            Contacts = contacts.Select(c => new ContactRow(c.Id, c.CreatedAt, c.Type,
                c.Type == ContactType.CallRequest ? "Arama isteği" : PresetMessages.TextFor(c.PresetKey) ?? "-",
                c.Result, VisitorLabel(c.IpHash), blockedHashes.Contains(c.IpHash))).ToList(),
            Blocked = blocked.Select(b => new BlockedRow(b.Id, VisitorLabel(b.IpHash), b.CreatedAt)).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Block(int id)
    {
        var ownerId = OwnerId;
        var contact = await context.ContactRequests.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.Sticker!.OwnerId == ownerId);
        if (contact is null)
            return NotFound();

        if (!await context.BlockedVisitors.AnyAsync(b => b.StickerId == contact.StickerId && b.IpHash == contact.IpHash))
        {
            context.BlockedVisitors.Add(new BlockedVisitor { StickerId = contact.StickerId, IpHash = contact.IpHash });
            await context.SaveChangesAsync();
        }

        TempData["Success"] = "Ziyaretçi bu etiket için engellendi.";
        return RedirectToAction(nameof(Details), new { id = contact.StickerId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unblock(int id)
    {
        var ownerId = OwnerId;
        var blocked = await context.BlockedVisitors.FirstOrDefaultAsync(b => b.Id == id && b.Sticker!.OwnerId == ownerId);
        if (blocked is null)
            return NotFound();

        context.BlockedVisitors.Remove(blocked);
        await context.SaveChangesAsync();

        TempData["Success"] = "Engel kaldırıldı.";
        return RedirectToAction(nameof(Details), new { id = blocked.StickerId });
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

    // A short label so an owner can tell repeat visitors apart without seeing any address
    private static string VisitorLabel(string ipHash) => ipHash[..Math.Min(6, ipHash.Length)];

    private static string Vehicle(string plate, string? nickname) =>
        string.IsNullOrEmpty(nickname) ? Plates.Mask(plate) : $"{nickname} ({Plates.Mask(plate)})";
}
