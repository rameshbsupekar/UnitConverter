using Microsoft.AspNetCore.Builder;

namespace UnitConverter.Auth.API.Middleware;

/// <summary>
/// Extension methods for registering Auth API middleware in the application pipeline.
/// Provides a convenient method to add all auth-related middleware in the correct order.
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Registers all authentication and security middleware in the application pipeline.
    /// Order is critical for proper functionality:
    /// 1. ExceptionHandlingMiddleware - Catches and maps exceptions to HTTP responses
    /// 2. SecurityHeadersMiddleware - Adds security headers to responses
    /// 3. RequestLoggingMiddleware - Logs requests and generates correlation IDs
    /// 4. AuditLoggingMiddleware - Audits authentication-related endpoints
    /// </summary>
    /// <param name="app">The IApplicationBuilder to configure.</param>
    /// <returns>The IApplicationBuilder for method chaining.</returns>
    public static IApplicationBuilder UseAuthMiddleware(this IApplicationBuilder app)
    {
        app = app ?? throw new ArgumentNullException(nameof(app));

        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<AuditLoggingMiddleware>();

        return app;
    }
}
