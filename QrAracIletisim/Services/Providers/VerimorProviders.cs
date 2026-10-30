namespace QrAracIletisim.Services.Providers;

// SKELETON ONLY: these classes make no network calls and hold no real keys.
// They mark where a paid Verimor account plugs in; each method fails on purpose until it is implemented.
// To finish them: take the request formats from Verimor's API documentation, call the API with an HttpClient
// from IHttpClientFactory, and read credentials from environment variables or a secret store, never from source control.

public class VerimorOptions
{
    public const string SectionName = "Verimor";

    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string SourceNumber { get; set; } = "";
}

public class VerimorSmsSender : ISmsSender
{
    // TODO: send through Verimor's SMS API with an approved sender name (SourceNumber)
    public Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Verimor SMS entegrasyonu henüz yapılmadı.");
}

public class VerimorWhatsAppSender : IWhatsAppSender
{
    // TODO: send through Verimor's WhatsApp service; business messages need pre-approved templates
    public Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Verimor WhatsApp entegrasyonu henüz yapılmadı.");
}

public class VerimorCallMaskingService : ICallMaskingService
{
    // TODO: reserve a virtual number that forwards to the owner until the expiry time
    public Task<MaskedCall> CreateMaskedCallAsync(string ownerPhoneNumber, TimeSpan validFor, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Verimor numara maskeleme entegrasyonu henüz yapılmadı.");
}
