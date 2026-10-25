using System.Security.Cryptography;
using QrAracIletisim.Data;
using QrAracIletisim.Models;

namespace QrAracIletisim.Services.Providers;

// Free stand-ins for the paid services: nothing leaves the machine, every message is logged and kept in the outbox table

public abstract class FakeProviderBase(ApplicationDbContext context, ILogger logger)
{
    public const string ProviderName = "Fake";

    protected async Task RecordAsync(OutboxChannel channel, string to, string body, CancellationToken cancellationToken)
    {
        logger.LogInformation("[Fake {Channel}] to {To}: {Body}", channel, PhoneNumbers.Mask(to), body);

        context.OutboxMessages.Add(new OutboxMessage
        {
            Channel = channel,
            To = to,
            Body = body,
            Provider = ProviderName
        });
        await context.SaveChangesAsync(cancellationToken);
    }
}

public class FakeSmsSender(ApplicationDbContext context, ILogger<FakeSmsSender> logger)
    : FakeProviderBase(context, logger), ISmsSender
{
    public Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default) =>
        RecordAsync(OutboxChannel.Sms, phoneNumber, message, cancellationToken);
}

public class FakeWhatsAppSender(ApplicationDbContext context, ILogger<FakeWhatsAppSender> logger)
    : FakeProviderBase(context, logger), IWhatsAppSender
{
    public Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default) =>
        RecordAsync(OutboxChannel.WhatsApp, phoneNumber, message, cancellationToken);
}

public class FakeCallMaskingService(ApplicationDbContext context, ILogger<FakeCallMaskingService> logger)
    : FakeProviderBase(context, logger), ICallMaskingService
{
    public async Task<MaskedCall> CreateMaskedCallAsync(string ownerPhoneNumber, TimeSpan validFor, CancellationToken cancellationToken = default)
    {
        // A made-up 0850 number in the same shape a masking provider would return
        var virtualNumber = $"0850 {RandomNumberGenerator.GetInt32(100, 1000)} {RandomNumberGenerator.GetInt32(10, 100)} {RandomNumberGenerator.GetInt32(10, 100)}";
        var call = new MaskedCall(virtualNumber, DateTime.UtcNow.Add(validFor));

        await RecordAsync(OutboxChannel.Call, ownerPhoneNumber,
            $"Maskeli arama hattı açıldı: {virtualNumber} numarasını arayan kişi {validFor.TotalMinutes:0} dakika boyunca size bağlanır.",
            cancellationToken);
        return call;
    }
}
