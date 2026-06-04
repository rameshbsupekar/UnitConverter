using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using UnitConverter.Common.Constants;

namespace UnitConverter.Common.Middleware;

/// <summary>
/// Logs requests with correlation and trace identifiers.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ExtractOrGenerateCorrelationId(context);
        context.Items[HttpHeaderNames.CorrelationId] = correlationId;
        context.Response.Headers[HttpHeaderNames.CorrelationId] = correlationId;

        var traceId = ExtractOrGenerateTraceId(context);
        context.Items[HttpHeaderNames.TraceId] = traceId;
        context.Response.Headers[HttpHeaderNames.TraceId] = traceId;

        var activity = Activity.Current;
        if (activity is not null)
        {
            activity.AddTag(OpenTelemetryTagNames.CorrelationId, correlationId);
            activity.AddTag(OpenTelemetryTagNames.TraceId, traceId);
        }

        _logger.LogInformation(
            "HTTP Request: {Method} {Path} | CorrelationId: {CorrelationId} | TraceId: {TraceId}",
            context.Request.Method,
            context.Request.Path,
            correlationId,
            traceId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            _logger.LogInformation(
                "HTTP Response: {Method} {Path} {StatusCode} | Duration: {DurationMs}ms | CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                correlationId);
        }
    }

    private static string ExtractOrGenerateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HttpHeaderNames.CorrelationId, out var correlationId))
        {
            var id = correlationId.ToString();
            if (!string.IsNullOrWhiteSpace(id))
            {
                return id;
            }
        }

        return Guid.NewGuid().ToString("D");
    }

    private static string ExtractOrGenerateTraceId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HttpHeaderNames.TraceId, out var traceId))
        {
            var id = traceId.ToString();
            if (!string.IsNullOrWhiteSpace(id))
            {
                return id;
            }
        }

        return context.TraceIdentifier;
    }
}
