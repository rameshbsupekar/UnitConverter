using Microsoft.Extensions.Logging;
using UnitConverter.Common.Constants;
using UnitConverter.Common.Http;
using UnitConverter.UserManagement.Core.Domain.Exceptions;

namespace UnitConverter.UserManagement.Api.Middleware;

/// <summary>
/// Maps auth-specific domain exceptions. General exceptions are handled by <see cref="Common.Middleware.ApiExceptionHandlingMiddleware"/>.
/// </summary>
public sealed class AuthDomainExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthDomainExceptionHandlingMiddleware> _logger;

    public AuthDomainExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<AuthDomainExceptionHandlingMiddleware> logger)
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
        catch (UserAlreadyExistsException ex)
        {
            _logger.LogWarning(ex, "User already exists");
            await ProblemDetailsResponseWriter.WriteAsync(
                context,
                StatusCodes.Status409Conflict,
                ProblemTitles.Conflict,
                ex.Message);
        }
    }
}
