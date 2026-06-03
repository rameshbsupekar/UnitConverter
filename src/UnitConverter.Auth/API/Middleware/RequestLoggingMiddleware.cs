using Microsoft.AspNetCore.Http;
using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace UnitConverter.Auth.API.Middleware;

/// <summary>
/// Middleware for logging HTTP requests and responses with correlation and trace IDs.
/// Generates or extracts X-Correlation-ID header for distributed tracing.
/// Propagates trace IDs for OpenTelemetry integration.
/// Logs request summary including duration, method, path, and status code.
/// </summary>
public class RequestLoggingMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private const string TraceIdHeaderName = "X-Trace-ID";

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract or generate Correlation ID
        var correlationId = ExtractOrGenerateCorrelationId(context);
        context.Items[CorrelationIdHeaderName] = correlationId;
        context.Response.Headers[CorrelationIdHeaderName] = correlationId;

        // Extract or generate Trace ID
        var traceId = ExtractOrGenerateTraceId(context);
        context.Items[TraceIdHeaderName] = traceId;
        context.Response.Headers[TraceIdHeaderName] = traceId;

        // Store in Activity for OpenTelemetry
        var activity = Activity.Current;
        if (activity != null)
        {
            activity.AddTag("correlation.id", correlationId);
            activity.AddTag("trace.id", traceId);
        }

        // Log request start
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

            // Log request completion with duration
            _logger.LogInformation(
                "HTTP Response: {Method} {Path} {StatusCode} | Duration: {DurationMs}ms | CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                correlationId);
        }
    }

    /// <summary>
    /// Extracts existing Correlation ID or generates a new GUID-based one.
    /// </summary>
    private static string ExtractOrGenerateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId))
        {
            var id = correlationId.ToString();
            if (!string.IsNullOrWhiteSpace(id))
            {
                return id;
            }
        }

        return Guid.NewGuid().ToString("D");
    }

    /// <summary>
    /// Extracts existing Trace ID or uses the HttpContext TraceIdentifier.
    /// Trace ID is used for OpenTelemetry correlation.
    /// </summary>
    private static string ExtractOrGenerateTraceId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(TraceIdHeaderName, out var traceId))
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
