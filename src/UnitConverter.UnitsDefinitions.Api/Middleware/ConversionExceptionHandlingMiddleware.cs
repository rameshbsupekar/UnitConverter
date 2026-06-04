using UnitConverter.Common.Constants;
using UnitConverter.Common.Http;
using UnitConverter.UnitsDefinitions.Conversion;

namespace UnitConverter.UnitsDefinitions.Api.Middleware;

/// <summary>
/// Maps <see cref="ConversionException"/> to RFC 7807 problem responses.
/// </summary>
public sealed class ConversionExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ConversionExceptionHandlingMiddleware(RequestDelegate next) =>
        _next = next ?? throw new ArgumentNullException(nameof(next));

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ConversionException ex)
        {
            await ProblemDetailsResponseWriter.WriteAsync(
                context,
                StatusCodes.Status400BadRequest,
                ProblemTitles.BadRequest,
                ex.Message);
        }
    }
}
