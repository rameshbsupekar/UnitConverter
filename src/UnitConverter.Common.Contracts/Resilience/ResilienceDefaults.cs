namespace UnitConverter.Common.Contracts.Resilience;

/// <summary>
/// Default resilience tuning values (no magic numbers in implementations).
/// </summary>
public static class ResilienceDefaults
{
    public const int HttpMaxRetryAttempts = 3;
    public const int HttpExternalMaxRetryAttempts = 4;
    public const int HttpRetryDelayMilliseconds = 100;
    public const int HttpAttemptTimeoutSeconds = 3;
    public const int HttpTotalTimeoutSeconds = 10;
    public const int HttpExternalTotalTimeoutSeconds = 30;
    public const int CircuitBreakerMinimumThroughput = 10;
    public const int CircuitBreakerExternalMinimumThroughput = 5;
    public const int CircuitBreakerSamplingSeconds = 30;
    public const int CircuitBreakerExternalSamplingSeconds = 60;
    public const double CircuitBreakerFailureRatio = 0.5;
}
