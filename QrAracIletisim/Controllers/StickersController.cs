using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QrAracIletisim.Data;
using QrAracIletisim.Models;
using QrAracIletisim.Models.ViewModels;
using QrAracIletisim.Services;

namespace QrAracIletisim.Controllers;

[Authorize(Roles = Roles.Admin)]
public class StickersController(ApplicationDbContext context, IConfiguration configuration) : Controller
{
    public async Task<IActionResult> Index() => View(await BuildIndexAsync(new GenerateStickersViewModel()));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate([Bind(Prefix = "Generate")] GenerateStickersViewModel model)
    {
        if (!ModelState.IsValid)
            return View(nameof(Index), await BuildIndexAsync(model));

        var batch = new StickerBatch
        {
            Count = model.Count,
            Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim(),
            CreatedById = User.FindFirstValue(ClaimTypes.NameIdentifier)!
        };
        for (var i = 0; i < model.Count; i++)
            batch.Stickers.Add(new Sticker { Token = StickerTokens.Generate() });

        context.StickerBatches.Add(batch);
        await context.SaveChangesAsync();

        TempData["Success"] = $"{model.Count} etiket üretildi.";
        return RedirectToAction(nameof(Index));
    }

    // A sheet to print and cut; tokens appear only inside the QR codes
    public async Task<IActionResult> Print(int id)
    {
        var batch = await context.StickerBatches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
        if (batch is null)
            return NotFound();

        var tokens = await context.Stickers.AsNoTracking()
            .Where(s => s.BatchId == id)
            .OrderBy(s => s.Id)
            .Select(s => s.Token)
            .ToListAsync();

        ViewData["Title"] = $"Parti #{batch.Id}";
        return View(tokens.Select(t => new PrintableSticker(QrCode.Svg(StickerTokens.PublicUrl(Request, configuration, t)))).ToList());
    }

    private async Task<StickersIndexViewModel> BuildIndexAsync(GenerateStickersViewModel generate) => new()
    {
        Generate = generate,
        Batches = await context.StickerBatches.AsNoTracking()
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BatchRow(b.Id, b.CreatedAt, b.Count, b.Note, b.Stickers.Count(s => s.Status != StickerStatus.Unclaimed)))
            .ToListAsync()
    };
}
