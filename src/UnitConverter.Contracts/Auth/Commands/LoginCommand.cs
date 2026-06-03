namespace UnitConverter.Contracts.Auth.Commands;

/// <summary>
/// Command to authenticate a user with email and password
/// </summary>
public record LoginCommand(
    string Email,
    string Password
);
