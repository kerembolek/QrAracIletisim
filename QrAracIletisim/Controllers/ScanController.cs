using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using QrAracIletisim.Data;
using QrAracIletisim.Models;
using QrAracIletisim.Models.ViewModels;
using QrAracIletisim.Services;

namespace QrAracIletisim.Controllers;

// The public page behind the QR code: no sign-in, and nothing about the owner except a masked plate
[Route("s/{token}")]
public class ScanController(ApplicationDbContext context, ContactService contactService, SecureHash secureHash) : Controller
{
    private const string ResultKey = "ContactResult";
    private const string MaskedNumberKey = "MaskedNumber";

    [HttpGet]
    [EnableRateLimiting(RateLimits.Scan)]
    public async Task<IActionResult> Index(string token)
    {
        var sticker = await context.Stickers.Include(s => s.Vehicle).FirstOrDefaultAsync(s => s.Token == token);
        if (sticker is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("Invalid");
        }
        if (sticker.Status == StickerStatus.Unclaimed)
            return RedirectToAction("Index", "Claim", new { token });

        context.ScanEvents.Add(new ScanEvent { StickerId = sticker.Id, IpHash = VisitorHash() });
        await context.SaveChangesAsync();

        return View(new ScanViewModel(token, Plates.Mask(sticker.Vehicle?.Plate), sticker.Status));
    }

    [HttpPost("message")]
    [EnableRateLimiting(RateLimits.Contact)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Message(string token, string? presetKey)
    {
        // Only keys from the fixed list get through, so a forged form cannot send custom text to an owner
        if (PresetMessages.TextFor(presetKey) is null)
            return BadRequest();

        return await SubmitAsync(token, ContactType.Message, presetKey);
    }

    [HttpPost("call")]
    [EnableRateLimiting(RateLimits.Contact)]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Call(string token) => SubmitAsync(token, ContactType.CallRequest, presetKey: null);

    // A separate page, so refreshing it neither resends the message nor counts as a new scan
    [HttpGet("sent")]
    public IActionResult Sent(string token)
    {
        if (TempData[ResultKey] is not int result)
            return RedirectToAction(nameof(Index), new { token });

        ViewData["MaskedNumber"] = TempData[MaskedNumberKey];
        ViewData["Token"] = token;
        return View((ContactResult)result);
    }

    private async Task<IActionResult> SubmitAsync(string token, ContactType type, string? presetKey)
    {
        var sticker = await context.Stickers
            .Include(s => s.Owner)
            .Include(s => s.Vehicle)
            .FirstOrDefaultAsync(s => s.Token == token);
        if (sticker is null || sticker.Status == StickerStatus.Unclaimed)
            return NotFound();

        var outcome = await contactService.SubmitAsync(sticker, type, presetKey, VisitorHash());

        TempData[ResultKey] = (int)outcome.Result;
        TempData[MaskedNumberKey] = outcome.MaskedNumber;
        return RedirectToAction(nameof(Sent), new { token });
    }

    private string VisitorHash() => secureHash.Compute($"ip:{RateLimits.ClientIp(HttpContext)}");
}
