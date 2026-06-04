using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using UnitConverter.Common.Http;

namespace UnitConverter.Common.DependencyInjection;

/// <summary>
/// ASP.NET Core global exception pipeline (developer HTML page in Development, safe JSON otherwise).
/// </summary>
public static class GlobalExceptionApplicationBuilderExtensions
{
    /// <summary>
    /// Development: HTML developer exception page (full stack trace and exception message).
    /// Non-development: RFC 7807 JSON fallback via <see cref="ProblemDetailsResponseWriter.WriteExceptionHandlerResponseAsync"/>.
    /// Expected client errors (validation, domain) are mapped by <see cref="Middleware.ApiExceptionHandlingMiddleware"/> before reaching this handler.
    /// </summary>
    public static IApplicationBuilder UseUnitConverterAspNetExceptionHandling(
        this IApplicationBuilder app,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(environment);

        if (environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler(errorApp =>
                errorApp.Run(ProblemDetailsResponseWriter.WriteExceptionHandlerResponseAsync));
        }

        return app;
    }
}
