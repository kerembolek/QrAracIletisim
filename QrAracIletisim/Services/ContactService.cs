using Microsoft.EntityFrameworkCore;
using QrAracIletisim.Data;
using QrAracIletisim.Models;
using QrAracIletisim.Services.Providers;

namespace QrAracIletisim.Services;

public record ContactOutcome(ContactResult Result, string? MaskedNumber = null);

// Decides whether a visitor's message or call request reaches the owner, then records the attempt either way
public class ContactService(
    ApplicationDbContext context,
    ISmsSender smsSender,
    IWhatsAppSender whatsAppSender,
    ICallMaskingService callMaskingService,
    ILogger<ContactService> logger)
{
    public const int VisitorHourlyLimit = 10;
    public static readonly TimeSpan RepeatCooldown = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan MaskedCallLifetime = TimeSpan.FromMinutes(10);

    // The sticker must come with its Owner and Vehicle loaded
    public async Task<ContactOutcome> SubmitAsync(Sticker sticker, ContactType type, string? presetKey, string ipHash)
    {
        var outcome = new ContactOutcome(await CheckAsync(sticker, ipHash));
        if (outcome.Result == ContactResult.Delivered)
            outcome = await DeliverAsync(sticker, type, presetKey);

        context.ContactRequests.Add(new ContactRequest
        {
            StickerId = sticker.Id,
            Type = type,
            PresetKey = presetKey,
            IpHash = ipHash,
            Result = outcome.Result,
            MaskedNumber = outcome.MaskedNumber
        });
        await context.SaveChangesAsync();
        return outcome;
    }

    private async Task<ContactResult> CheckAsync(Sticker sticker, string ipHash)
    {
        if (sticker.Status != StickerStatus.Active)
            return ContactResult.Refused;

        if (await context.BlockedVisitors.AnyAsync(b => b.StickerId == sticker.Id && b.IpHash == ipHash))
            return ContactResult.Blocked;

        var now = DateTime.UtcNow;
        var hourAgo = now.AddHours(-1);
        var delivered = context.ContactRequests.Where(c => c.Result == ContactResult.Delivered);

        if (await delivered.CountAsync(c => c.StickerId == sticker.Id && c.CreatedAt >= hourAgo) >= sticker.HourlyMessageLimit)
            return ContactResult.RateLimited;

        var cooldownStart = now - RepeatCooldown;
        if (await delivered.AnyAsync(c => c.StickerId == sticker.Id && c.IpHash == ipHash && c.CreatedAt >= cooldownStart))
            return ContactResult.RateLimited;

        if (await delivered.CountAsync(c => c.IpHash == ipHash && c.CreatedAt >= hourAgo) >= VisitorHourlyLimit)
            return ContactResult.RateLimited;

        return ContactResult.Delivered;
    }

    private async Task<ContactOutcome> DeliverAsync(Sticker sticker, ContactType type, string? presetKey)
    {
        var owner = sticker.Owner!;
        var vehicle = Plates.Mask(sticker.Vehicle?.Plate);
        try
        {
            string? maskedNumber = null;
            string notice;
            if (type == ContactType.CallRequest)
            {
                var call = await callMaskingService.CreateMaskedCallAsync(owner.PhoneNumber!, MaskedCallLifetime);
                maskedNumber = call.VirtualNumber;
                notice = $"Aracınız ({vehicle}) için arama isteği var. Sizi gizli bir hattan arayacaklar.";
            }
            else
            {
                notice = $"Aracınız ({vehicle}) için mesaj: {PresetMessages.TextFor(presetKey)}";
            }

            if (owner.NotifyViaWhatsApp)
                await whatsAppSender.SendAsync(owner.PhoneNumber!, notice);
            else
                await smsSender.SendAsync(owner.PhoneNumber!, notice);

            return new ContactOutcome(ContactResult.Delivered, maskedNumber);
        }
        catch (Exception ex)
        {
            // A provider failure must not show the visitor an error page; the attempt is recorded as refused
            logger.LogError(ex, "Notification failed for sticker {StickerId}", sticker.Id);
            return new ContactOutcome(ContactResult.Refused);
        }
    }
}
