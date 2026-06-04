using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace UnitConverter.Common.Contracts.Resilience;

/// <summary>
/// Configures rate limiting and HTTP resilience (implemented in Infrastructure).
/// </summary>
public interface IResilienceConfigurer
{
    /// <summary>Registers ASP.NET Core rate limiting policies from configuration.</summary>
    void AddRateLimiting(IServiceCollection services, IConfiguration configuration);

    /// <summary>Applies retry, timeout, and circuit breaker handlers to an HTTP client builder.</summary>
    void AddHttpResilience(IHttpClientBuilder builder);
}
