namespace UnitConverter.Contracts.Auth.Commands;

/// <summary>
/// Command to refresh an expired access token
/// </summary>
public record RefreshTokenCommand(
    string RefreshToken
);
