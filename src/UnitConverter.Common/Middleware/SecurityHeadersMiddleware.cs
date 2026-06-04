using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using UnitConverter.Common.Constants;

namespace UnitConverter.Common.Middleware;

/// <summary>
/// Adds HSTS, CSP, and related security headers to responses.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        ILogger<SecurityHeadersMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers.StrictTransportSecurity = SecurityHeaderValues.StrictTransportSecurity;
        headers.XFrameOptions = SecurityHeaderValues.XFrameOptionsDeny;
        headers.XContentTypeOptions = SecurityHeaderValues.XContentTypeOptions;
        headers[HttpResponseHeaderNames.ReferrerPolicy] = ReferrerPolicyValues.StrictOriginWhenCrossOrigin;

        headers.ContentSecurityPolicy = _environment.EnvironmentName == Environments.Production
            ? SecurityHeaderValues.StrictContentSecurityPolicy
            : SecurityHeaderValues.DevelopmentContentSecurityPolicy;

        _logger.LogDebug("Security headers applied to response");

        await _next(context);
    }
}
