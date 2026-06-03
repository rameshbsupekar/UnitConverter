namespace UnitConverter.Auth.Application.Commands;

/// <summary>
/// Command to register a new user in the system.
/// Includes all required information and idempotency key for retry safety.
/// </summary>
public sealed class RegisterUserCommand
{
    /// <summary>
    /// User's email address (must be unique).
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// User's password (will be hashed).
    /// </summary>
    public string Password { get; set; } = null!;

    /// <summary>
    /// User's first name.
    /// </summary>
    public string FirstName { get; set; } = null!;

    /// <summary>
    /// User's last name.
    /// </summary>
    public string LastName { get; set; } = null!;

    /// <summary>
    /// Organization name the user belongs to.
    /// </summary>
    public string OrganizationName { get; set; } = null!;

    /// <summary>
    /// Idempotency key (Guid) for retry safety.
    /// Ensures that duplicate requests return the same result.
    /// </summary>
    public string IdempotencyKey { get; set; } = null!;
}
