using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UnitConverter.Core.Interfaces;
using UnitConverter.Core.Resilience;

namespace UnitConverter.Core.Extensions;

/// <summary>
/// Extension methods for configuring Core resilience services.
/// 
/// These extensions provide a clean API for any consuming service to add
/// rate limiting and HTTP resilience without coupling to Core implementation details.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Core resilience configuration (rate limiting and HTTP resilience) to the service collection.
    /// 
    /// This method encapsulates all resilience setup and can be called from any service
    /// (Auth.Api, Catalog.Api, Conversion.Api, etc.) independently.
    /// 
    /// No service-specific logic is included here; all configuration is generic.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for fluent chaining.</returns>
    /// <remarks>
    /// Usage in Program.cs:
    /// <code>
    /// builder.Services.AddCoreResilience(builder.Configuration);
    /// </code>
    /// </remarks>
    public static IServiceCollection AddCoreResilience(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Create and register the resilience configurer
        var configurer = new ResilienceConfigurer();

        // Configure rate limiting policies
        configurer.AddRateLimiting(services, configuration);

        // Register the configurer interface for dependency injection
        // This allows services to inject IResilienceConfigurer if they need to
        services.AddSingleton<IResilienceConfigurer>(configurer);

        return services;
    }

    /// <summary>
    /// Adds HTTP resilience to an HttpClientBuilder.
    /// 
    /// This is a convenience method for configuring HTTP resilience on individual HTTP clients.
    /// Can be chained after AddHttpClient() calls.
    /// </summary>
    /// <param name="builder">The HttpClientBuilder.</param>
    /// <returns>The updated HttpClientBuilder.</returns>
    /// <remarks>
    /// Usage in Program.cs:
    /// <code>
    /// builder.Services
    ///     .AddHttpClient("CatalogService", client => 
    ///         client.BaseAddress = new Uri(config["Services:Catalog:Url"]))
    ///     .AddCoreHttpResilience();
    /// </code>
    /// </remarks>
    public static IHttpClientBuilder AddCoreHttpResilience(
        this IHttpClientBuilder builder)
    {
        var configurer = new ResilienceConfigurer();
        configurer.AddHttpResilience(builder);
        return builder;
    }
}
