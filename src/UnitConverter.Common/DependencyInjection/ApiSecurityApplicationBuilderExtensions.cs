using Microsoft.AspNetCore.Builder;
using UnitConverter.Common.Middleware;

namespace UnitConverter.Common.DependencyInjection;

/// <summary>
/// Shared security middleware pipeline for API hosts (headers, logging, error handling).
/// </summary>
public static class ApiSecurityApplicationBuilderExtensions
{
    /// <summary>
    /// Shared pipeline for all API hosts: <see cref="Middleware.ApiExceptionHandlingMiddleware"/> → security headers → request logging.
    /// Call after <c>UseRouting</c> and <c>UseRateLimiter</c>. Register host-specific domain middleware after this method.
    /// </summary>
    public static IApplicationBuilder UseUnitConverterApiSecurity(this IApplicationBuilder app)
    {
        app.UseMiddleware<ApiExceptionHandlingMiddleware>();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        return app;
    }
}
