using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace QrAracIletisim.Services;

// Per-IP throttling that stops floods before they reach the database
public static class RateLimits
{
    public const string Scan = "scan";
    public const string Contact = "contact";
    public const string Sms = "sms";

    public static IServiceCollection AddPublicRateLimits(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
                await context.HttpContext.Response.WriteAsync("Çok fazla istek gönderildi. Lütfen biraz sonra tekrar deneyin.", cancellationToken);
            };
            AddPerIpPolicy(options, Scan, permitsPerMinute: 60);
            AddPerIpPolicy(options, Contact, permitsPerMinute: 10);
            AddPerIpPolicy(options, Sms, permitsPerMinute: 5);
        });

    public static string ClientIp(HttpContext context) => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static void AddPerIpPolicy(RateLimiterOptions options, string name, int permitsPerMinute) =>
        options.AddPolicy(name, context => RateLimitPartition.GetFixedWindowLimiter(ClientIp(context), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitsPerMinute,
            Window = TimeSpan.FromMinutes(1)
        }));
}
