using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

namespace UnitConverter.Common.DependencyInjection;

/// <summary>
/// Request-size and transport hardening (ADR-0003 abuse-resistance hooks).
/// </summary>
public static class ApiSecurityServiceCollectionExtensions
{
    private const int DefaultMaxRequestBodyBytes = 32 * 1024;

    public static IServiceCollection AddApiSecurity(
        this IServiceCollection services,
        long maxRequestBodyBytes = DefaultMaxRequestBodyBytes)
    {
        services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = maxRequestBodyBytes;
            options.ValueLengthLimit = (int)Math.Min(maxRequestBodyBytes, int.MaxValue);
        });

        services.Configure<KestrelServerOptions>(options =>
        {
            options.Limits.MaxRequestBodySize = maxRequestBodyBytes;
        });

        return services;
    }
}
