using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using UnitConverter.Common.Constants;
using UnitConverter.Common.Http;

namespace UnitConverter.Common.Middleware;

/// <summary>
/// Shared API exception mapper (User Management and Units Definitions hosts).
/// Validation and bad-request types return safe client messages.
/// Unhandled exceptions: Development rethrows for the developer exception page; Production returns a generic 500 detail.
/// </summary>
public sealed class ApiExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ApiExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ApiExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            var detail = ValidationErrorFormatter.Format(ex);
            _logger.LogWarning(ex, "Validation failed: {Detail}", detail);
            await ProblemDetailsResponseWriter.WriteAsync(
                context,
                StatusCodes.Status400BadRequest,
                ProblemTitles.BadRequest,
                detail);
        }
        catch (BadHttpRequestException ex)
        {
            _logger.LogWarning(ex, "Bad request: {Message}", ex.Message);
            await ProblemDetailsResponseWriter.WriteAsync(
                context,
                StatusCodes.Status400BadRequest,
                ProblemTitles.BadRequest,
                ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            await ProblemDetailsResponseWriter.WriteAsync(
                context,
                StatusCodes.Status401Unauthorized,
                ProblemTitles.Unauthorized,
                ProblemTitles.AccessDeniedDetail);
        }
        catch (RateLimitException ex)
        {
            _logger.LogWarning(ex, "Rate limit exceeded");
            context.Response.Headers[HeaderNames.RetryAfter] = ex.RetryAfterSeconds.ToString();
            await ProblemDetailsResponseWriter.WriteAsync(
                context,
                StatusCodes.Status429TooManyRequests,
                ProblemTitles.TooManyRequests,
                ProblemTitles.RateLimitDetail);
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            _logger.LogError(ex, "Unhandled exception of type {ExceptionType}", ex.GetType().Name);

            if (_environment.IsDevelopment())
            {
                throw;
            }

            await ProblemDetailsResponseWriter.WriteAsync(
                context,
                StatusCodes.Status500InternalServerError,
                ProblemTitles.InternalServerError,
                ProblemTitles.UnexpectedErrorDetail);
        }
    }
}
