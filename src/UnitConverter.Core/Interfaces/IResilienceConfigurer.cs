using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace UnitConverter.Core.Interfaces;

/// <summary>
/// Defines contracts for configuring resilience policies across services.
/// 
/// This interface enables any service to register rate limiting and HTTP resilience
/// without coupling to UnitConverter.Core implementation details.
/// 
/// Benefits:
/// - Any service (Auth, Catalog, Conversion) can implement custom resilience
/// - Testing: easily mock resilience configuration
/// - Decoupling: Core doesn't know about consuming services
/// </summary>
public interface IResilienceConfigurer
{
    /// <summary>
    /// Configures rate limiting policies in the service collection.
    /// 
    /// Policies:
    /// - FixedWindow_ByIp: 10 req/sec per IP (public endpoints)
    /// - SlidingWindow_ByUser: 100 req/min per user (authenticated endpoints)
    /// - TokenBucket_ByApiKey: 500 req/min per API key (partner/external)
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="config">The application configuration containing RateLimiting settings.</param>
    void AddRateLimiting(IServiceCollection services, IConfiguration config);

    /// <summary>
    /// Configures HTTP resilience (retry, circuit breaker, timeouts) for an HTTP client.
    /// 
    /// Standard pipeline:
    /// Total Timeout → Retry (exponential backoff) → Circuit Breaker → Attempt Timeout
    /// </summary>
    /// <param name="builder">The HttpClientBuilder to configure.</param>
    void AddHttpResilience(IHttpClientBuilder builder);
}
