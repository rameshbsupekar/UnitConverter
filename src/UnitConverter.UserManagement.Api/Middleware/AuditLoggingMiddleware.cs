using System.Diagnostics;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc.Controllers;
using UnitConverter.Common.Attributes;
using UnitConverter.Common.Constants;

namespace UnitConverter.UserManagement.Api.Middleware;

/// <summary>
/// Audit logging for endpoints marked with <see cref="AuditedAttribute"/>.
/// Requires <c>UseRouting()</c> earlier in the pipeline so <see cref="HttpContext.GetEndpoint"/> is set.
/// </summary>
public sealed class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;

    public AuditLoggingMiddleware(
        RequestDelegate next,
        ILogger<AuditLoggingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldAudit(context))
        {
            await _next(context);
            return;
        }

        await InvokeWithAuditLoggingAsync(context);
    }

    private async Task InvokeWithAuditLoggingAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var stopwatch = Stopwatch.StartNew();
        var method = context.Request.Method;
        var correlationId = GetCorrelationId(context);
        var traceId = GetTraceId(context);
        var userId = ExtractUserId(context);
        var action = ResolveAuditAction(context);
        var apiVersion = ResolveApiVersion(context);

        _logger.LogInformation(
            "Audit: {Action} started | Path: {Path} | Method: {Method} | ApiVersion: {ApiVersion} | UserId: {UserId} | CorrelationId: {CorrelationId} | TraceId: {TraceId}",
            action,
            path,
            method,
            apiVersion ?? "unspecified",
            userId ?? AuditActionNames.AnonymousUser,
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
                "Audit: {Action} completed | Path: {Path} | StatusCode: {StatusCode} | Duration: {DurationMs}ms | ApiVersion: {ApiVersion} | UserId: {UserId} | CorrelationId: {CorrelationId}",
                action,
                path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                apiVersion ?? "unspecified",
                userId ?? AuditActionNames.AnonymousUser,
                correlationId);
        }
    }

    private static bool ShouldAudit(HttpContext context) =>
        context.GetEndpoint()?.Metadata.GetMetadata<AuditedAttribute>() is not null;

    private static string? ResolveApiVersion(HttpContext context)
    {
        if (context.RequestServices is null)
        {
            return null;
        }

        try
        {
            return context.GetRequestedApiVersion()?.ToString();
        }
        catch (ArgumentNullException)
        {
            return null;
        }
    }

    private static string ResolveAuditAction(HttpContext context)
    {
        var audited = context.GetEndpoint()?.Metadata.GetMetadata<AuditedAttribute>();
        if (!string.IsNullOrWhiteSpace(audited?.Action))
        {
            return audited.Action;
        }

        var actionName = context.GetEndpoint()?.Metadata
            .GetMetadata<ControllerActionDescriptor>()?.ActionName;

        return MapActionName(actionName);
    }

    private static string MapActionName(string? actionName)
    {
        if (string.IsNullOrWhiteSpace(actionName))
        {
            return AuditActionNames.AuthAction;
        }

        if (actionName.Contains(AuthControllerActionFragments.Register, StringComparison.OrdinalIgnoreCase))
        {
            return AuditActionNames.UserRegistration;
        }

        if (actionName.Contains(AuthControllerActionFragments.Login, StringComparison.OrdinalIgnoreCase))
        {
            return AuditActionNames.UserLogin;
        }

        if (actionName.Contains(AuthControllerActionFragments.Logout, StringComparison.OrdinalIgnoreCase))
        {
            return AuditActionNames.UserLogout;
        }

        return AuditActionNames.AuthAction;
    }

    private static string? ExtractUserId(HttpContext context)
    {
        return context.User?.FindFirst(JwtClaimTypes.Subject)?.Value
            ?? context.User?.FindFirst(JwtClaimTypes.NameIdentifier)?.Value
            ?? context.User?.FindFirst(JwtClaimTypes.UserId)?.Value;
    }

    private static string GetCorrelationId(HttpContext context)
    {
        if (context.Items.TryGetValue(HttpHeaderNames.CorrelationId, out var correlationIdObj))
        {
            return correlationIdObj?.ToString() ?? context.TraceIdentifier;
        }

        return context.Request.Headers.TryGetValue(HttpHeaderNames.CorrelationId, out var correlationId)
            ? correlationId.ToString()
            : context.TraceIdentifier;
    }

    private static string GetTraceId(HttpContext context)
    {
        if (context.Items.TryGetValue(HttpHeaderNames.TraceId, out var traceIdObj))
        {
            return traceIdObj?.ToString() ?? context.TraceIdentifier;
        }

        return context.Request.Headers.TryGetValue(HttpHeaderNames.TraceId, out var traceId)
            ? traceId.ToString()
            : context.TraceIdentifier;
    }
}
