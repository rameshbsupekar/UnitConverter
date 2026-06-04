namespace UnitConverter.Common.Constants;

/// <summary>
/// Custom / extension HTTP header names (not in <see cref="Microsoft.Net.Http.Headers.HeaderNames"/>).
/// For standard headers use <see cref="Microsoft.Net.Http.Headers.HeaderNames"/>.
/// </summary>
public static class HttpHeaderNames
{
    public const string CorrelationId = "X-Correlation-ID";
    public const string TraceId = "X-Trace-ID";
}
