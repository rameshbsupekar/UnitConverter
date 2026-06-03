namespace UnitConverter.Contracts.Auth.Commands;

/// <summary>
/// Command to revoke a refresh token
/// </summary>
public record RevokeTokenCommand(
    string RefreshToken
);
