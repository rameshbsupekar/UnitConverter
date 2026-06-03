namespace UnitConverter.Contracts.Auth.Commands;

/// <summary>
/// Command to register a new user account
/// </summary>
public record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string OrganizationName
);
