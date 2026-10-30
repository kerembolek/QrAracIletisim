namespace QrAracIletisim.Services.Providers;

public static class ProviderServiceCollectionExtensions
{
    // "Providers:Mode" picks the implementation; the rest of the app only knows the interfaces
    public static IServiceCollection AddMessagingProviders(this IServiceCollection services, IConfiguration configuration)
    {
        var mode = configuration["Providers:Mode"] ?? "Fake";
        switch (mode)
        {
            case "Fake":
                services.AddScoped<ISmsSender, FakeSmsSender>();
                services.AddScoped<IWhatsAppSender, FakeWhatsAppSender>();
                services.AddScoped<ICallMaskingService, FakeCallMaskingService>();
                break;
            case "Verimor":
                services.Configure<VerimorOptions>(configuration.GetSection(VerimorOptions.SectionName));
                services.AddScoped<ISmsSender, VerimorSmsSender>();
                services.AddScoped<IWhatsAppSender, VerimorWhatsAppSender>();
                services.AddScoped<ICallMaskingService, VerimorCallMaskingService>();
                break;
            default:
                throw new InvalidOperationException($"Bilinmeyen Providers:Mode değeri: {mode}");
        }
        return services;
    }
}
