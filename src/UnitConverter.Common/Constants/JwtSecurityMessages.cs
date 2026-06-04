namespace UnitConverter.Common.Constants;

/// <summary>
/// Startup and configuration error messages for JWT authentication.
/// </summary>
public static class JwtSecurityMessages
{
    public const string SecretRequired = "Jwt:Secret is required.";
    public const string SecretMinLengthFormat = "Jwt:Secret must be at least {0} characters.";
}
