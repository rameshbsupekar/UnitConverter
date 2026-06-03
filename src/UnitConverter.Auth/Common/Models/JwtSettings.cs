using System;

namespace UnitConverter.Auth.Common.Models;

/// <summary>
/// Configuration settings for JWT token generation and validation.
/// Should be loaded from configuration (appsettings.json).
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Secret key used to sign tokens. Must be at least 32 characters.
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Issuer of the token (typically the application name or domain).
    /// Example: https://unitconverter.example.com
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Audience for the token (who should accept the token).
    /// Example: unitconverter-api
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Access token expiry time in minutes. Default: 15 minutes.
    /// </summary>
    public int AccessTokenExpiryMinutes { get; set; } = 15;

    /// <summary>
    /// Refresh token expiry time in days. Default: 7 days.
    /// </summary>
    public int RefreshTokenExpiryDays { get; set; } = 7;
}
