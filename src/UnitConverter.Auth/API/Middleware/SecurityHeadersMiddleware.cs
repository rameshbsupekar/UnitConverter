using Microsoft.AspNetCore.Http;
using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace UnitConverter.Auth.API.Middleware;

/// <summary>
/// Middleware for adding security headers to HTTP responses.
/// Implements HSTS, CSP, X-Frame-Options, X-Content-Type-Options, and Referrer-Policy.
/// Configuration is environment-aware (stricter in Production).
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public SecurityHeadersMiddleware(RequestDelegate next, ILogger<SecurityHeadersMiddleware> logger, IWebHostEnvironment environment)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Strict-Transport-Security (HSTS)
        // Tells browsers to use HTTPS only for 1 year (including subdomains)
        context.Response.Headers.StrictTransportSecurity = "max-age=31536000; includeSubDomains; preload";

        // X-Frame-Options: DENY
        // Prevents clickjacking attacks by disallowing embedding in frames
        context.Response.Headers.XFrameOptions = "DENY";

        // X-Content-Type-Options: nosniff
        // Prevents MIME type sniffing attacks
        context.Response.Headers.XContentTypeOptions = "nosniff";

        // Referrer-Policy: strict-origin-when-cross-origin
        // Controls referrer information sent with requests
        context.Response.Headers.ReferrerPolicy = "strict-origin-when-cross-origin";

        // Content-Security-Policy (CSP)
        // Environment-aware: stricter in Production, more permissive in Development
        var cspHeader = _environment.IsProduction()
            ? GetStrictCsp()
            : GetDevelopmentCsp();

        context.Response.Headers.ContentSecurityPolicy = cspHeader;

        _logger.LogDebug("Security headers applied to response");

        await _next(context);
    }

    /// <summary>
    /// Production CSP: Strict, no inline scripts, no dynamic eval.
    /// </summary>
    private static string GetStrictCsp()
    {
        return "default-src 'self'; " +
               "script-src 'self'; " +
               "style-src 'self' 'unsafe-inline'; " +
               "img-src 'self' data: https:; " +
               "font-src 'self'; " +
               "connect-src 'self'; " +
               "frame-ancestors 'none'; " +
               "base-uri 'self'; " +
               "form-action 'self'; " +
               "upgrade-insecure-requests";
    }

    /// <summary>
    /// Development CSP: More permissive to aid debugging and development tools.
    /// </summary>
    private static string GetDevelopmentCsp()
    {
        return "default-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
               "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
               "style-src 'self' 'unsafe-inline'; " +
               "img-src 'self' data: https: http:; " +
               "font-src 'self' data:; " +
               "connect-src 'self' http: https: ws: wss:; " +
               "frame-ancestors 'none'";
    }
}
