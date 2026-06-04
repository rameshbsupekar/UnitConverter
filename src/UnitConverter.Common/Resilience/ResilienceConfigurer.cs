using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Resilience;
using UnitConverter.Common.Contracts.Resilience;

namespace UnitConverter.Common.Resilience;

/// <inheritdoc />
public sealed class ResilienceConfigurer : IResilienceConfigurer
{
    private const string PoliciesSection = "RateLimiting:Policies";

    /// <inheritdoc />
    public void AddRateLimiting(IServiceCollection services, IConfiguration configuration)
    {
        var policies = configuration.GetSection(PoliciesSection);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(RateLimitPolicyNames.FixedWindowByIp, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetClientIp(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = GetPolicyInt(policies, "FixedWindow_ByIp", "PermitLimit", 100),
                        QueueLimit = GetPolicyInt(policies, "FixedWindow_ByIp", "QueueLimit", 0),
                        Window = TimeSpan.FromSeconds(
                            GetPolicyInt(policies, "FixedWindow_ByIp", "WindowSeconds", 60)),
                        AutoReplenishment = true
                    }));

            options.AddPolicy(RateLimitPolicyNames.SlidingWindowByUser, httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    GetUserPartitionKey(httpContext),
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = GetPolicyInt(policies, "SlidingWindow_ByUser", "PermitLimit", 100),
                        QueueLimit = GetPolicyInt(policies, "SlidingWindow_ByUser", "QueueLimit", 0),
                        Window = TimeSpan.FromSeconds(
                            GetPolicyInt(policies, "SlidingWindow_ByUser", "WindowSeconds", 60)),
                        SegmentsPerWindow = GetPolicyInt(policies, "SlidingWindow_ByUser", "SegmentsPerWindow", 4),
                        AutoReplenishment = true
                    }));

            options.AddPolicy(RateLimitPolicyNames.TokenBucketByApiKey, httpContext =>
                RateLimitPartition.GetTokenBucketLimiter(
                    GetApiKeyPartitionKey(httpContext),
                    _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = GetPolicyInt(policies, "TokenBucket_ByApiKey", "TokenLimit", 500),
                        QueueLimit = GetPolicyInt(policies, "TokenBucket_ByApiKey", "QueueLimit", 0),
                        ReplenishmentPeriod = TimeSpan.FromSeconds(
                            GetPolicyInt(policies, "TokenBucket_ByApiKey", "ReplenishmentPeriodSeconds", 60)),
                        TokensPerPeriod = GetPolicyInt(policies, "TokenBucket_ByApiKey", "TokensPerPeriod", 500),
                        AutoReplenishment = true
                    }));
        });
    }

#pragma warning disable S2325 // Instance method implements IResilienceConfigurer for DI registration
    public void AddHttpResilience(IHttpClientBuilder builder, int maxRetryAttempts = ResilienceDefaults.HttpMaxRetryAttempts) =>
        HttpResilienceConfigurer.Apply(builder, maxRetryAttempts);
#pragma warning restore S2325

    /// <inheritdoc />
    void IResilienceConfigurer.AddHttpResilience(IHttpClientBuilder builder) =>
        HttpResilienceConfigurer.Apply(builder, ResilienceDefaults.HttpMaxRetryAttempts);

    private static int GetPolicyInt(IConfigurationSection policies, string policyName, string key, int defaultValue) =>
        policies.GetSection(policyName).GetValue(key, defaultValue);

    private static string GetClientIp(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static string GetUserPartitionKey(HttpContext httpContext) =>
        httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? httpContext.User.Identity?.Name
        ?? "anonymous";

    private static string GetApiKeyPartitionKey(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue("X-Api-Key", out var apiKey)
            && !string.IsNullOrWhiteSpace(apiKey))
        {
            return apiKey.ToString();
        }

        return "anonymous";
    }
}

internal static class HttpResilienceConfigurer
{
    internal static void Apply(IHttpClientBuilder builder, int maxRetryAttempts)
    {
        builder.AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = maxRetryAttempts;
            options.Retry.UseJitter = true;
            options.Retry.Delay = TimeSpan.FromMilliseconds(ResilienceDefaults.HttpRetryDelayMilliseconds);

            options.CircuitBreaker.MinimumThroughput = ResilienceDefaults.CircuitBreakerMinimumThroughput;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(ResilienceDefaults.CircuitBreakerSamplingSeconds);
            options.CircuitBreaker.FailureRatio = ResilienceDefaults.CircuitBreakerFailureRatio;

            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(ResilienceDefaults.HttpAttemptTimeoutSeconds);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(ResilienceDefaults.HttpTotalTimeoutSeconds);
        });
    }
}
