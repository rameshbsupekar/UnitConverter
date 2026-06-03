using System;

namespace UnitConverter.Auth.Core.Domain.Entities;

/// <summary>
/// Role entity - represents a role in the system.
/// Predefined roles: Admin, Partner, Employee, Public
/// Simple structure - no complex business logic.
/// </summary>
public class Role
{
    /// <summary>
    /// Unique identifier for the role.
    /// </summary>
    public int Id { get; private set; }

    /// <summary>
    /// Role name (e.g., "Admin", "Partner", "Employee", "Public").
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Role description - explains what this role does.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// When this role was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Private parameterless constructor for EF Core.
    /// </summary>
    private Role()
    {
    }

    /// <summary>
    /// Factory method to create a new Role.
    /// </summary>
    public static Role Create(int id, string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Role name cannot be empty.", nameof(name));
        }

        return new Role
        {
            Id = id,
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Equality based on role ID.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not Role other)
        {
            return false;
        }

        return Id == other.Id;
    }

    /// <summary>
    /// Hash code based on role ID.
    /// </summary>
    public override int GetHashCode() => Id.GetHashCode();

    /// <summary>
    /// String representation.
    /// </summary>
    public override string ToString() => Name;
}
