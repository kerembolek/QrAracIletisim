namespace QrAracIletisim.Services.Providers;

// Each outside service sits behind its own interface, so the app runs on the free fake providers
// and a paid provider (e.g. Verimor) can be plugged in through configuration without touching controllers

public interface ISmsSender
{
    Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
}

public interface IWhatsAppSender
{
    Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
}

public interface ICallMaskingService
{
    // Reserves a virtual number that forwards to the owner for a short time; neither side sees the other's real number
    Task<MaskedCall> CreateMaskedCallAsync(string ownerPhoneNumber, TimeSpan validFor, CancellationToken cancellationToken = default);
}

public record MaskedCall(string VirtualNumber, DateTime ExpiresAt);
