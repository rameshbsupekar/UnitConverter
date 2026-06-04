using System;
using System.Collections.Generic;
using System.Linq;
using UnitConverter.UserManagement.Core.Domain.ValueObjects;

namespace UnitConverter.UserManagement.Core.Domain.Entities;

/// <summary>
/// User aggregate root - represents a user in the Auth Service.
/// Responsibilities:
/// - User identity and profile
/// - Authentication (password hash storage - NOT plaintext)
/// - Authorization (roles)
/// - Lifecycle management (active/inactive)
/// 
/// Key principles:
/// - Uniquely identified by UserId
/// - Immutable creation (via factory method)
/// - Business logic for updates
/// - All dependencies injected for testability
/// </summary>
public class User
{
    /// <summary>
    /// Unique identifier for this user (aggregate root).
    /// </summary>
    public UserId Id { get; private set; } = null!;

    /// <summary>
    /// User's email address (unique across system).
    /// </summary>
    public Email Email { get; private set; } = null!;

    /// <summary>
    /// User's first name.
    /// </summary>
    public string FirstName { get; private set; } = null!;

    /// <summary>
    /// User's last name.
    /// </summary>
    public string LastName { get; private set; } = null!;

    /// <summary>
    /// Organization name the user belongs to.
    /// Can be their company, department, or self-identifier.
    /// </summary>
    public string OrganizationName { get; private set; } = null!;

    /// <summary>
    /// Bcrypt or other secure hash of the password.
    /// NEVER store plaintext passwords.
    /// </summary>
    public string PasswordHash { get; private set; } = null!;

    /// <summary>
    /// Roles assigned to this user.
    /// </summary>
    public ICollection<Role> Roles { get; private set; } = new List<Role>();

    /// <summary>
    /// When this user account was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When this user account was last modified.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Whether this user account is active.
    /// Inactive users cannot authenticate.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Private parameterless constructor for EF Core.
    /// </summary>
    private User()
    {
    }

    /// <summary>
    /// Factory method to create a new User aggregate.
    /// All parameters validated before entity creation.
    /// </summary>
    public static User Create(
        long userId,
        string email,
        string firstName,
        string lastName,
        string organizationName,
        string passwordHash)
    {
        // Validate all inputs
        var userIdVo = UserId.Create(userId);
        var emailVo = Email.Create(email);

        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty.", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be empty.", nameof(lastName));

        if (string.IsNullOrWhiteSpace(organizationName))
            throw new ArgumentException("Organization name cannot be empty.", nameof(organizationName));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty.", nameof(passwordHash));

        return new User
        {
            Id = userIdVo,
            Email = emailVo,
            FirstName = firstName,
            LastName = lastName,
            OrganizationName = organizationName,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true,
            Roles = new List<Role>()
        };
    }

    /// <summary>
    /// Update user''s email address.
    /// </summary>
    public void UpdateEmail(string newEmail)
    {
        if (!IsActive)
            throw new InvalidOperationException("Cannot update inactive user.");

        Email = Email.Create(newEmail);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update user''s password hash (after secure hashing).
    /// </summary>
    public void UpdatePasswordHash(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("Password hash cannot be empty.", nameof(newPasswordHash));

        if (!IsActive)
            throw new InvalidOperationException("Cannot update inactive user.");

        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivate this user account.
    /// Deactivated users cannot authenticate.
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive)
            throw new InvalidOperationException("User is already inactive.");

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reactivate this user account.
    /// </summary>
    public void Activate()
    {
        if (IsActive)
            throw new InvalidOperationException("User is already active.");

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Assign a role to this user.
    /// </summary>
    public void AssignRole(Role role)
    {
        if (role is null)
            throw new ArgumentNullException(nameof(role));

        if (Roles.Any(r => r.Id == role.Id))
            throw new InvalidOperationException($"User already has role: {role.Name}");

        Roles.Add(role);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Remove a role from this user.
    /// </summary>
    public void RemoveRole(Role role)
    {
        if (role is null)
            throw new ArgumentNullException(nameof(role));

        var existingRole = Roles.FirstOrDefault(r => r.Id == role.Id);
        if (existingRole is null)
            throw new InvalidOperationException($"User does not have role: {role.Name}");

        Roles.Remove(existingRole);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if user has a specific role by name.
    /// </summary>
    public bool HasRole(string roleName)
    {
        return Roles.Any(r => r.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Equality based on UserId (aggregate identity).
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not User other)
        {
            return false;
        }

        return Id == other.Id;
    }

    /// <summary>
    /// Hash code based on UserId.
    /// </summary>
    public override int GetHashCode() => Id.GetHashCode();
}
