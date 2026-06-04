using Microsoft.AspNetCore.Builder;

namespace UnitConverter.UnitsDefinitions.Api.Middleware;

/// <summary>
/// Units Definitions host middleware (domain exceptions). Call
/// <see cref="Common.DependencyInjection.ApiSecurityApplicationBuilderExtensions.UseUnitConverterApiSecurity"/>
/// before this for shared RFC 7807 handling, headers, and request logging.
/// </summary>
public static class UnitsDefinitionsMiddlewareExtensions
{
    /// <summary>
    /// Conversion and unit-catalog domain exception mapping (inner pipeline).
    /// </summary>
    public static IApplicationBuilder UseUnitsDefinitionsDomainExceptionHandling(this IApplicationBuilder app)
    {
        app.UseMiddleware<ConversionExceptionHandlingMiddleware>();
        app.UseMiddleware<UnitCatalogExceptionHandlingMiddleware>();
        return app;
    }
}
