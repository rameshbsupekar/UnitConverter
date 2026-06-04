namespace UnitConverter.Common.Constants;

/// <summary>
/// Security response header values.
/// </summary>
public static class SecurityHeaderValues
{
    public const string StrictTransportSecurity = "max-age=31536000; includeSubDomains; preload";
    public const string XContentTypeOptions = "nosniff";
    public const string XFrameOptionsDeny = "DENY";

    public static readonly string StrictContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self'; " +
        "img-src 'self' data: https:; " +
        "font-src 'self'; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "upgrade-insecure-requests";

    public static readonly string DevelopmentContentSecurityPolicy =
        "default-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: https: http:; " +
        "font-src 'self' data:; " +
        "connect-src 'self' http: https: ws: wss:; " +
        "frame-ancestors 'none'";
}
