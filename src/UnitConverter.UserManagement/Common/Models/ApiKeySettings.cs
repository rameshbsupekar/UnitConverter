using System;

namespace UnitConverter.UserManagement.Common.Models;

/// <summary>
/// Configuration settings for API key management and policies.
/// Should be loaded from configuration (appsettings.json).
/// </summary>
public class ApiKeySettings
{
    /// <summary>
    /// API key expiry time in days. Default: 365 days (1 year).
    /// </summary>
    public int ApiKeyExpiryDays { get; set; } = 365;

    /// <summary>
    /// Maximum number of active API keys per partner. Default: 5.
    /// </summary>
    public int MaxApiKeysPerPartner { get; set; } = 5;

    /// <summary>
    /// Whether to require periodic API key rotation. Default: true.
    /// </summary>
    public bool RequireApiKeyRotation { get; set; } = true;
}
