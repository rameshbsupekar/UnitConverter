namespace UnitConverter.UserManagement.Contracts.Responses;

/// <summary>
/// Response containing registered or authenticated user information.
/// </summary>
public sealed record UserResponse(
    long Id,
    string Email,
    string FirstName,
    string LastName,
    string OrganizationName,
    DateTime CreatedAt,
    string[] Roles);
