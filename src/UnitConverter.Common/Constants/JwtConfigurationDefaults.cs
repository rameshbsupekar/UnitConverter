namespace UnitConverter.Common.Constants;

/// <summary>
/// Default JWT settings for local development when configuration is omitted.
/// </summary>
public static class JwtConfigurationDefaults
{
    public const string Issuer = "https://localhost:7180";
    public const string Audience = "unitconverter-api";
    public const int MinSecretLength = 32;
}
