using UnitConverter.Common.Constants;

namespace UnitConverter.Common.Middleware;

/// <summary>
/// Thrown when an application-level rate limit is exceeded (maps to HTTP 429).
/// </summary>
public sealed class RateLimitException : Exception
{
    public int RetryAfterSeconds { get; }

    public RateLimitException(string message, int retryAfterSeconds = RateLimitDefaults.DefaultRetryAfterSeconds)
        : base(message)
    {
        RetryAfterSeconds = retryAfterSeconds;
    }
}
