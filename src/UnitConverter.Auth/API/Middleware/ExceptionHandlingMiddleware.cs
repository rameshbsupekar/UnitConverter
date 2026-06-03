using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace UnitConverter.Auth.API.Middleware;

/// <summary>
/// Middleware for handling exceptions and mapping them to RFC 7807 ProblemDetails responses.
/// Maps exceptions to appropriate HTTP status codes (400, 401, 403, 429, 500).
/// Includes correlation ID and structured logging for traceability.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BadHttpRequestException ex)
        {
            _logger.LogWarning(ex, "Bad request: {Message}", ex.Message);
            await WriteProblemDetails(context, StatusCodes.Status400BadRequest, "Bad Request", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            await WriteProblemDetails(context, StatusCodes.Status401Unauthorized, "Unauthorized", "Access denied");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation: {Message}", ex.Message);
            await WriteProblemDetails(context, StatusCodes.Status403Forbidden, "Forbidden", ex.Message);
        }
        catch (RateLimitException ex)
        {
            _logger.LogWarning(ex, "Rate limit exceeded");
            context.Response.Headers.RetryAfter = ex.RetryAfterSeconds.ToString();
            await WriteProblemDetails(context, StatusCodes.Status429TooManyRequests, "Too Many Requests", "Rate limit exceeded");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception of type {ExceptionType}", ex.GetType().Name);
            await WriteProblemDetails(context, StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred");
        }
    }

    private static async Task WriteProblemDetails(HttpContext context, int statusCode, string title, string detail)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var problemDetails = new
        {
            type = $"https://api.unitconverter.dev/errors/{statusCode}",
            title,
            status = statusCode,
            detail,
            traceId = context.TraceIdentifier,
            correlationId = GetCorrelationId(context),
            timestamp = DateTime.UtcNow.ToString("O")
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsJsonAsync(problemDetails, options);
    }

    private static string GetCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            return correlationId.ToString();
        }

        return context.TraceIdentifier;
    }
}

/// <summary>
/// Custom exception for rate limiting scenarios.
/// </summary>
public class RateLimitException : Exception
{
    public int RetryAfterSeconds { get; }

    public RateLimitException(string message, int retryAfterSeconds = 60) : base(message)
    {
        RetryAfterSeconds = retryAfterSeconds;
    }
}
