namespace UnitConverter.UserManagement.Contracts.Requests;

/// <summary>
/// HTTP request body for user login.
/// </summary>
public sealed record LoginRequest(
    string Email,
    string Password);
