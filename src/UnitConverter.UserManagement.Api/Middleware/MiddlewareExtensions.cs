using Microsoft.AspNetCore.Builder;

namespace UnitConverter.UserManagement.Api.Middleware;

/// <summary>
/// Extension methods for registering Auth API middleware in the application pipeline.
/// Provides a convenient method to add all auth-related middleware in the correct order.
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Auth-only middleware (audit). Call <see cref="Common.DependencyInjection.ApiSecurityApplicationBuilderExtensions.UseUnitConverterApiSecurity"/>
    /// before this for shared exception handling, headers, and request logging.
    /// </summary>
    public static IApplicationBuilder UseAuthMiddleware(this IApplicationBuilder app)
    {
        app = app ?? throw new ArgumentNullException(nameof(app));

        app.UseMiddleware<AuthDomainExceptionHandlingMiddleware>();
        app.UseMiddleware<AuditLoggingMiddleware>();

        return app;
    }
}
