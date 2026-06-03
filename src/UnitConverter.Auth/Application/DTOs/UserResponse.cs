using System;

namespace UnitConverter.Auth.Application.DTOs;

/// <summary>
/// Response DTO for user registration and queries.
/// Contains user profile information after successful registration.
/// </summary>
public sealed class UserResponse
{
    /// <summary>
    /// User's unique identifier.
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// User's email address.
    /// </summary>
    public string Email { get; set; } = null!;

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
    /// When this user account was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
