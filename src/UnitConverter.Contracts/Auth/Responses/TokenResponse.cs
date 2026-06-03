namespace UnitConverter.Contracts.Auth.Responses;

/// <summary>
/// Response containing JWT tokens after successful authentication
/// </summary>
public record TokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    DateTime IssuedAt
);
