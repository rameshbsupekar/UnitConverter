using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UnitConverter.Common.Constants;

namespace UnitConverter.Common.DependencyInjection;

/// <summary>
/// CORS for browser clients (ADR-0003). Supports explicit origins plus any localhost port when enabled.
/// </summary>
public static class CorsServiceCollectionExtensions
{
    public const string AllowedOriginsSection = "Cors:AllowedOrigins";
    public const string AllowAnyLocalhostPortKey = "Cors:AllowAnyLocalhostPort";

    /// <summary>
    /// Registers the default CORS policy when explicit origins and/or localhost-any-port is enabled.
    /// </summary>
    public static IServiceCollection AddUnitConverterCors(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var options = ResolveCorsOptions(configuration, environment);
        if (!options.IsEnabled)
        {
            return services;
        }

        var explicitOrigins = options.ExplicitOrigins;

        services.AddCors(cors =>
        {
            cors.AddPolicy(CorsPolicyNames.Default, policy =>
            {
                policy.SetIsOriginAllowed(origin =>
                {
                    if (options.AllowAnyLocalhostPort && IsLocalhostOrigin(origin))
                    {
                        return true;
                    }

                    return explicitOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
                })
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// Applies CORS when <see cref="AddUnitConverterCors"/> registered a policy.
    /// Place after <c>UseRouting</c> and before <c>UseAuthorization</c>.
    /// </summary>
    public static IApplicationBuilder UseUnitConverterCors(this IApplicationBuilder app)
    {
        var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
        var environment = app.ApplicationServices.GetRequiredService<IHostEnvironment>();

        if (ResolveCorsOptions(configuration, environment).IsEnabled)
        {
            app.UseCors(CorsPolicyNames.Default);
        }

        return app;
    }

    internal static CorsOptions ResolveCorsOptions(IConfiguration configuration, IHostEnvironment environment)
    {
        var explicitOrigins = GetExplicitOrigins(configuration);
        var allowAnyLocalhostPort = configuration.GetValue(AllowAnyLocalhostPortKey, defaultValue: false);

        if (!allowAnyLocalhostPort
            && environment.IsDevelopment()
            && !environment.IsEnvironment(HostEnvironmentNames.Testing)
            && explicitOrigins.Count == 0)
        {
            allowAnyLocalhostPort = true;
        }

        return new CorsOptions(explicitOrigins, allowAnyLocalhostPort);
    }

    internal static bool IsLocalhostOrigin(string origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || uri.Host.Equals("[::1]", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> GetExplicitOrigins(IConfiguration configuration)
    {
        var configured = configuration.GetSection(AllowedOriginsSection).Get<string[]>();
        if (configured is not { Length: > 0 })
        {
            return Array.Empty<string>();
        }

        return configured
            .Where(static o => !string.IsNullOrWhiteSpace(o))
            .Select(static o => o.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    internal readonly record struct CorsOptions(
        IReadOnlyList<string> ExplicitOrigins,
        bool AllowAnyLocalhostPort)
    {
        public bool IsEnabled => AllowAnyLocalhostPort || ExplicitOrigins.Count > 0;
    }
}
