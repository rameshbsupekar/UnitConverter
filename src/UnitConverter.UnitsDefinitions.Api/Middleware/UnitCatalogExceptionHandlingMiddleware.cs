using Microsoft.Extensions.Logging;
using UnitConverter.Common.Constants;
using UnitConverter.Common.Http;
using UnitConverter.UnitsDefinitions.Catalog;

namespace UnitConverter.UnitsDefinitions.Api.Middleware;

/// <summary>
/// Maps unit catalog domain exceptions to RFC 7807 problem responses.
/// </summary>
public sealed class UnitCatalogExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UnitCatalogExceptionHandlingMiddleware> _logger;

    public UnitCatalogExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<UnitCatalogExceptionHandlingMiddleware> logger)
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
        catch (UnitDuplicateException ex)
        {
            _logger.LogWarning(ex, "Unit catalog conflict");
            await ProblemDetailsResponseWriter.WriteAsync(
                context,
                StatusCodes.Status409Conflict,
                ProblemTitles.Conflict,
                ex.Message);
        }
        catch (UnitNotFoundException ex)
        {
            _logger.LogWarning(ex, "Unit catalog resource not found");
            await ProblemDetailsResponseWriter.WriteAsync(
                context,
                StatusCodes.Status404NotFound,
                ProblemTitles.NotFound,
                ex.Message);
        }
        catch (UnitCatalogException ex)
        {
            _logger.LogWarning(ex, "Unit catalog validation failed");
            await ProblemDetailsResponseWriter.WriteAsync(
                context,
                StatusCodes.Status400BadRequest,
                ProblemTitles.BadRequest,
                ex.Message);
        }
    }
}
