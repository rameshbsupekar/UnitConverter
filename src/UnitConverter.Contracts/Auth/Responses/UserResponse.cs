namespace UnitConverter.Contracts.Auth.Responses;

/// <summary>
/// Response containing authenticated user information
/// </summary>
public record UserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string[] Roles
);
