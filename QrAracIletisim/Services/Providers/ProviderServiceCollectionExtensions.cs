namespace QrAracIletisim.Services.Providers;

public static class ProviderServiceCollectionExtensions
{
    public static IServiceCollection AddMessagingProviders(this IServiceCollection services)
    {
        services.AddScoped<ISmsSender, FakeSmsSender>();
        services.AddScoped<IWhatsAppSender, FakeWhatsAppSender>();
        services.AddScoped<ICallMaskingService, FakeCallMaskingService>();
        return services;
    }
}
