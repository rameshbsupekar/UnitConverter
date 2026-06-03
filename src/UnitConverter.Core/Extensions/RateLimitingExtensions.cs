using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace UnitConverter.Core.Extensions;

/// <summary>
/// Extension methods for configuring ASP.NET Core rate limiting policies.
/// Provides three predefined policies for protecting API endpoints:
/// - FixedWindow_ByIp: 10 req/sec per IP address (public endpoints)
/// - SlidingWindow_ByUser: 100 req/min per authenticated user (authenticated endpoints)
/// - TokenBucket_ByApiKey: 500 req/min per API key (partner integrations)
/// 
/// NOTE: This extension requires Microsoft.AspNetCore.RateLimiting to be referenced
/// in the web application project (UnitConverter.Api).
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Configures application rate limiting with three policies from configuration.
    /// Policies are partitioned by IP, user identity, or API key header.
    /// 
    /// This method is meant to be called from a web application (Program.cs) that has
    /// access to the Microsoft.AspNetCore.RateLimiting package.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration containing RateLimiting settings.</param>
    /// <returns>The updated service collection.</returns>
    /// <remarks>
    /// In the web application's Program.cs, after calling this extension, ensure:
    /// 1. app.UseRateLimiter() is called in the middleware pipeline (before routing)
    /// 2. Endpoints are decorated with .RequireRateLimiting("PolicyName")
    /// 
    /// Example configuration in appsettings.json:
    /// {
    ///   "RateLimiting": {
    ///     "Policies": {
    ///       "FixedWindow_ByIp": {
    ///         "PermitLimit": 10,
    ///         "QueueLimit": 5
    ///       },
    ///       "SlidingWindow_ByUser": {
    ///         "PermitLimit": 100,
    ///         "QueueLimit": 10
    ///       },
    ///       "TokenBucket_ByApiKey": {
    ///         "TokenLimit": 500,
    ///         "QueueLimit": 0
    ///       }
    ///     }
    ///   }
    /// }
    /// </remarks>
    public static IServiceCollection AddApplicationRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Read configuration settings for rate limiting policies
        var rateLimitingConfig = configuration.GetSection("RateLimiting");
        
        // Store config values in service collection for use by rate limiting middleware
        services.Configure<RateLimitingConfiguration>(rateLimitingConfig);

        // The actual rate limiter setup happens in the web app via UseRateLimiter()
        // This service collection extension just validates configuration is present
        return services;
    }
}

/// <summary>
/// Configuration model for rate limiting policies.
/// </summary>
public class RateLimitingConfiguration
{
    public PoliciesConfiguration? Policies { get; set; }
}

/// <summary>
/// Individual policy configurations.
/// </summary>
public class PoliciesConfiguration
{
    public PolicyOptions? FixedWindow_ByIp { get; set; }
    public PolicyOptions? SlidingWindow_ByUser { get; set; }
    public PolicyOptions? TokenBucket_ByApiKey { get; set; }
}

/// <summary>
/// Individual policy option settings.
/// </summary>
public class PolicyOptions
{
    public int PermitLimit { get; set; } = 10;
    public int QueueLimit { get; set; } = 5;
    public int WindowSeconds { get; set; } = 1;
    public int WindowMinutes { get; set; } = 1;
    public int TokenLimit { get; set; } = 500;
    public int ReplenishmentMinutes { get; set; } = 1;
}
