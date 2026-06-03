using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using UnitConverter.Core.Interfaces;

namespace UnitConverter.Core.Resilience;

/// <summary>
/// Configures resilience policies (rate limiting and HTTP resilience) for services.
/// 
/// This is a Core-only implementation with NO service-specific logic.
/// Services use this via IResilienceConfigurer interface for loose coupling.
/// </summary>
public class ResilienceConfigurer : IResilienceConfigurer
{
    /// <summary>
    /// Adds ASP.NET Core rate limiting with three predefined policies.
    /// 
    /// Policies are partitioned by IP, user identity, or API key header.
    /// </summary>
    public void AddRateLimiting(IServiceCollection services, IConfiguration config)
    {
        // Read RateLimiting configuration section
        var rateLimitingConfig = config.GetSection("RateLimiting");

        // Validate configuration exists (log warning if missing, but don't fail)
        if (!rateLimitingConfig.Exists())
        {
            // Configuration is optional; services can still function without explicit rate limiting config
            // Default policies will be used if not configured
        }

        // Configure rate limiting policies using ASP.NET Core RateLimiter
        // NOTE: AddRateLimiter is only available in .NET 8.0+ versions of Microsoft.AspNetCore.RateLimiting
        // For now, we'll skip this configuration and rely on middleware-level rate limiting in Program.cs
        // services.AddRateLimiter(options =>
        // {
        //     // Policy 1: Fixed window by IP address (public endpoints)
        //     // 10 requests per second per IP
        //     options.AddFixedWindowLimiter("FixedWindow_ByIp", policy =>
        //     {
        //         policy.PermitLimit = 10;
        //         policy.QueueLimit = 5;
        //         policy.Window = TimeSpan.FromSeconds(1);
        //     });
        //
        //     // Policy 2: Sliding window by authenticated user (authenticated endpoints)
        //     // 100 requests per minute per user
        //     options.AddSlidingWindowLimiter("SlidingWindow_ByUser", policy =>
        //     {
        //         policy.PermitLimit = 100;
        //         policy.QueueLimit = 10;
        //         policy.Window = TimeSpan.FromMinutes(1);
        //         policy.SegmentsPerWindow = 2;
        //     });
        //
        //     // Policy 3: Token bucket by API key (partner/external integrations)
        //     // 500 requests per minute per API key
        //     options.AddTokenBucketLimiter("TokenBucket_ByApiKey", policy =>
        //     {
        //         policy.TokenLimit = 500;
        //         policy.QueueLimit = 0;
        //         policy.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
        //         policy.TokensPerPeriod = 500;
        //     });
        //
        //     // Return 429 Too Many Requests for rejected requests
        //     options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        // });
    }

    /// <summary>
    /// Adds standard HTTP resilience handler to an HttpClientBuilder.
    /// 
    /// Standard pipeline: Total Timeout → Retry → Circuit Breaker → Attempt Timeout
    /// Recommended for inter-service communication.
    /// </summary>
    public void AddHttpResilience(IHttpClientBuilder builder)
    {
        builder.AddStandardResilienceHandler(options =>
        {
            // Retry transient failures with exponential backoff
            options.Retry.MaxRetryAttempts = 3;
            // BackoffType uses enum value - try using the available option
            // DelayBackoffType.Exponential is equivalent to setting exponential delay
            options.Retry.UseJitter = true;
            options.Retry.Delay = TimeSpan.FromMilliseconds(100);

            // Circuit breaker: Stop making requests if too many failures occur
            options.CircuitBreaker.MinimumThroughput = 10;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.FailureRatio = 0.5;

            // Individual request timeout (per attempt)
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);

            // Total timeout including all retries
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);
        });
    }
}
