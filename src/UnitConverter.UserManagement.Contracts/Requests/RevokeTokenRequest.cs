namespace UnitConverter.UserManagement.Contracts.Requests;

/// <summary>
/// HTTP request body for revoking a refresh token.
/// </summary>
public sealed record RevokeTokenRequest(string RefreshToken);
