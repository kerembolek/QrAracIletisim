using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QrAracIletisim.Data;
using QrAracIletisim.Models;
using QrAracIletisim.Services;
using QrAracIletisim.Services.Providers;

namespace QrAracIletisim.Controllers;

[Authorize(Roles = Roles.Admin)]
public class OutboxController(
    ApplicationDbContext context,
    ISmsSender smsSender,
    IWhatsAppSender whatsAppSender,
    ICallMaskingService callMaskingService) : Controller
{
    private const int PageSize = 200;

    public async Task<IActionResult> Index(OutboxChannel? channel)
    {
        var query = context.OutboxMessages.AsNoTracking();
        if (channel is not null)
            query = query.Where(m => m.Channel == channel);

        ViewData["Channel"] = channel;
        return View(await query.OrderByDescending(m => m.CreatedAt).Take(PageSize).ToListAsync());
    }

    // Lets an admin check that the configured provider delivers, without going through the public page
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendTest(OutboxChannel channel, string? phoneNumber)
    {
        var phone = PhoneNumbers.Normalize(phoneNumber);
        if (phone is null)
        {
            TempData["Error"] = "Geçerli bir cep telefonu numarası girin (ör. 0532 123 45 67).";
            return RedirectToAction(nameof(Index));
        }

        const string body = "QR Araç İletişim deneme mesajı.";
        switch (channel)
        {
            case OutboxChannel.Sms:
                await smsSender.SendAsync(phone, body);
                break;
            case OutboxChannel.WhatsApp:
                await whatsAppSender.SendAsync(phone, body);
                break;
            case OutboxChannel.Call:
                await callMaskingService.CreateMaskedCallAsync(phone, TimeSpan.FromMinutes(10));
                break;
            default:
                return BadRequest();
        }

        TempData["Success"] = "Deneme gönderildi.";
        return RedirectToAction(nameof(Index));
    }
}
