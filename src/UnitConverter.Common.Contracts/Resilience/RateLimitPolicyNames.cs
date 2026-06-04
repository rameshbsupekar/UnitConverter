namespace UnitConverter.Common.Contracts.Resilience;

/// <summary>
/// ASP.NET Core rate limiter policy names.
/// </summary>
public static class RateLimitPolicyNames
{
    public const string FixedWindowByIp = "FixedWindow_ByIp";
    public const string SlidingWindowByUser = "SlidingWindow_ByUser";
    public const string TokenBucketByApiKey = "TokenBucket_ByApiKey";
}
