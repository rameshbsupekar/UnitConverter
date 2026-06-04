namespace UnitConverter.UserManagement.Contracts.Requests;

/// <summary>
/// HTTP request body for user registration.
/// </summary>
public sealed record RegisterUserRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string OrganizationName,
    string IdempotencyKey);
