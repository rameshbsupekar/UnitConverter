using Microsoft.AspNetCore.Http;
using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace UnitConverter.Auth.API.Middleware;

/// <summary>
/// Middleware for audit logging of authentication and authorization events.
/// Captures details about /register, /login, /logout actions including:
/// - User ID (when available)
/// - HTTP method and path
/// - Response status code
/// - Request duration
/// - Correlation ID and trace ID for traceability
/// </summary>
public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;

    // Paths that should be audit logged
    private static readonly string[] AuditedPaths = new[]
    {
        "/api/v1/auth/register",
        "/api/v1/auth/login",
        "/api/v1/auth/logout",
        "/api/auth/register",
        "/api/auth/login",
        "/api/auth/logout"
    };

    public AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        var shouldAudit = ShouldAuditPath(path);

        if (shouldAudit)
        {
            await InvokeWithAuditLogging(context);
        }
        else
        {
            await _next(context);
        }
    }

    private async Task InvokeWithAuditLogging(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var path = context.Request.Path.Value ?? "";
        var method = context.Request.Method;
        var correlationId = GetCorrelationId(context);
        var traceId = GetTraceId(context);
        var userId = ExtractUserId(context);

        _logger.LogInformation(
            "Audit: {Action} started | Path: {Path} | Method: {Method} | UserId: {UserId} | CorrelationId: {CorrelationId} | TraceId: {TraceId}",
            GetAction(path),
            path,
            method,
            userId ?? "ANONYMOUS",
            correlationId,
            traceId);

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            _logger.LogInformation(
                "Audit: {Action} completed | Path: {Path} | StatusCode: {StatusCode} | Duration: {DurationMs}ms | UserId: {UserId} | CorrelationId: {CorrelationId}",
                GetAction(path),
                path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                userId ?? "ANONYMOUS",
                correlationId);
        }
    }

    /// <summary>
    /// Determines if the request path should be audit logged.
    /// </summary>
    private static bool ShouldAuditPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return AuditedPaths.Any(auditedPath =>
            path.Equals(auditedPath, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(auditedPath + "/", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Extracts the user ID from the HttpContext (claims or session).
    /// Returns null if user is not authenticated.
    /// </summary>
    private static string? ExtractUserId(HttpContext context)
    {
        var userIdClaim = context.User?.FindFirst("sub")?.Value ??
                         context.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value ??
                         context.User?.FindFirst("uid")?.Value;

        return userIdClaim;
    }

    /// <summary>
    /// Gets the action name based on the request path.
    /// </summary>
    private static string GetAction(string path)
    {
        var normalizedPath = path.ToLowerInvariant();

        if (normalizedPath.Contains("register", StringComparison.OrdinalIgnoreCase))
            return "USER_REGISTRATION";

        if (normalizedPath.Contains("login", StringComparison.OrdinalIgnoreCase))
            return "USER_LOGIN";

        if (normalizedPath.Contains("logout", StringComparison.OrdinalIgnoreCase))
            return "USER_LOGOUT";

        return "AUTH_ACTION";
    }

    /// <summary>
    /// Gets the Correlation ID from context or HTTP headers.
    /// </summary>
    private static string GetCorrelationId(HttpContext context)
    {
        if (context.Items.TryGetValue("X-Correlation-ID", out var correlationIdObj))
        {
            return correlationIdObj?.ToString() ?? context.TraceIdentifier;
        }

        return context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId)
            ? correlationId.ToString()
            : context.TraceIdentifier;
    }

    /// <summary>
    /// Gets the Trace ID from context or HTTP headers.
    /// </summary>
    private static string GetTraceId(HttpContext context)
    {
        if (context.Items.TryGetValue("X-Trace-ID", out var traceIdObj))
        {
            return traceIdObj?.ToString() ?? context.TraceIdentifier;
        }

        return context.Request.Headers.TryGetValue("X-Trace-ID", out var traceId)
            ? traceId.ToString()
            : context.TraceIdentifier;
    }
}
