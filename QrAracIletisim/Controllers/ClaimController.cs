using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QrAracIletisim.Data;
using QrAracIletisim.Models;
using QrAracIletisim.Models.ViewModels;
using QrAracIletisim.Services;

namespace QrAracIletisim.Controllers;

[Route("claim/{token}")]
public class ClaimController(ApplicationDbContext context) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string token)
    {
        var sticker = await context.Stickers.AsNoTracking().FirstOrDefaultAsync(s => s.Token == token);
        if (sticker is null)
            return NotFound();
        if (sticker.Status != StickerStatus.Unclaimed)
            return AlreadyClaimed();

        ViewData["Token"] = token;
        return View(new ClaimViewModel());
    }

    [HttpPost]
    [Authorize(Roles = Roles.Owner)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(string token, ClaimViewModel model)
    {
        ViewData["Token"] = token;
        var sticker = await context.Stickers.AsNoTracking().FirstOrDefaultAsync(s => s.Token == token);
        if (sticker is null)
            return NotFound();
        if (sticker.Status != StickerStatus.Unclaimed)
            return AlreadyClaimed();

        var plate = Plates.Normalize(model.Plate);
        if (model.Plate is not null && plate is null)
            ModelState.AddModelError(nameof(model.Plate), "Geçerli bir plaka girin (ör. 34 ABC 123).");
        if (!ModelState.IsValid)
            return View(model);

        var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await using var transaction = await context.Database.BeginTransactionAsync();

        var vehicle = await context.Vehicles.FirstOrDefaultAsync(v => v.OwnerId == ownerId && v.Plate == plate);
        if (vehicle is null)
        {
            vehicle = new Vehicle { OwnerId = ownerId, Plate = plate!, Nickname = model.Nickname?.Trim() };
            context.Vehicles.Add(vehicle);
            await context.SaveChangesAsync();
        }

        // Updates only while still unclaimed, so two people scanning at once cannot both win the sticker
        var claimed = await context.Stickers
            .Where(s => s.Id == sticker.Id && s.Status == StickerStatus.Unclaimed)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, StickerStatus.Active)
                .SetProperty(x => x.OwnerId, ownerId)
                .SetProperty(x => x.VehicleId, vehicle.Id)
                .SetProperty(x => x.ClaimedAt, DateTime.UtcNow));
        if (claimed == 0)
            return AlreadyClaimed();

        await transaction.CommitAsync();
        TempData["Success"] = "Etiketiniz aracınıza bağlandı.";
        return RedirectToAction("Index", "MyStickers");
    }

    private RedirectToActionResult AlreadyClaimed()
    {
        TempData["Error"] = "Bu etiket daha önce sahiplenilmiş.";
        return RedirectToAction("Index", "Home");
    }
}
