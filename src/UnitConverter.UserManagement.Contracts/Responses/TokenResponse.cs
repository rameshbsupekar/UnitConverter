namespace UnitConverter.UserManagement.Contracts.Responses;

/// <summary>
/// OAuth-style token response.
/// </summary>
public sealed record TokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    DateTime IssuedAt);
