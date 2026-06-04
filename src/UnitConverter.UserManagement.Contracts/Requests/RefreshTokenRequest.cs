namespace UnitConverter.UserManagement.Contracts.Requests;

/// <summary>
/// HTTP request body for refreshing an access token.
/// </summary>
public sealed record RefreshTokenRequest(string RefreshToken);
