using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Polly;

namespace UnitConverter.Core.Extensions;

/// <summary>
/// Extension methods for configuring HTTP resilience handlers for outgoing requests.
/// Provides standard HTTP resilience pipeline using Microsoft.Extensions.Http.Resilience.
/// </summary>
public static class HttpResilienceExtensions
{
    /// <summary>
    /// Adds the standard HTTP resilience handler to an HttpClientBuilder.
    /// Standard pipeline: Total Timeout → Retry → Circuit Breaker → Attempt Timeout
    /// Recommended for most inter-service communication.
    /// </summary>
    /// <param name="builder">The HttpClientBuilder to configure.</param>
    /// <param name="maxRetries">Maximum number of retry attempts (default: 3).</param>
    /// <returns>The updated HttpClientBuilder.</returns>
    public static IHttpClientBuilder AddStandardServiceResilienceHandler(
        this IHttpClientBuilder builder,
        int maxRetries = 3)
    {
        builder.AddStandardResilienceHandler(options =>
        {
            // Retry transient failures with exponential backoff
            options.Retry.MaxRetryAttempts = maxRetries;
            options.Retry.BackoffType = DelayBackoffType.Exponential;
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

        return builder;
    }

    /// <summary>
    /// Adds a custom resilience handler for external APIs with strict limits.
    /// Pipeline: Total Timeout → Retry → Circuit Breaker → Attempt Timeout
    /// Recommended for calling third-party services (weather APIs, maps, etc.).
    /// </summary>
    /// <param name="builder">The HttpClientBuilder to configure.</param>
    /// <param name="pipelineName">The name of the resilience pipeline.</param>
    /// <param name="maxRetries">Maximum retry attempts (default: 4).</param>
    /// <param name="circuitBreakerThreshold">Minimum failures to trip circuit breaker (default: 5).</param>
    /// <returns>The updated HttpClientBuilder.</returns>
    public static IHttpClientBuilder AddExternalApiResilienceHandler(
        this IHttpClientBuilder builder,
        string pipelineName = "ExternalApi",
        int maxRetries = 4,
        int circuitBreakerThreshold = 5)
    {
        // Use standard handler for external APIs as well
        // The custom pipeline configuration would require additional package dependencies
        builder.AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = maxRetries;
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.UseJitter = true;
            options.Retry.Delay = TimeSpan.FromMilliseconds(100);

            options.CircuitBreaker.MinimumThroughput = circuitBreakerThreshold;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
            options.CircuitBreaker.FailureRatio = 0.5;

            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
        });

        return builder;
    }
}
