using System;

namespace UnitConverter.UserManagement.Core.Domain.ValueObjects;

/// <summary>
/// UserId value object - strongly typed identifier for User aggregate.
/// Ensures we don't confuse user IDs with other long values in the system.
/// </summary>
public sealed class UserId : IEquatable<UserId>
{
    /// <summary>
    /// The unique identifier value.
    /// </summary>
    public long Value { get; }

    /// <summary>
    /// Private constructor - instances created via static factory.
    /// </summary>
    private UserId(long value)
    {
        Value = value;
    }

    /// <summary>
    /// Static factory method to create UserId instance with validation.
    /// </summary>
    /// <param name="value">User ID value (must be > 0)</param>
    /// <returns>UserId value object instance</returns>
    /// <exception cref="ArgumentException">Thrown if value is not positive</exception>
    public static UserId Create(long value)
    {
        if (value <= 0)
        {
            throw new ArgumentException("UserId must be greater than 0.", nameof(value));
        }

        return new UserId(value);
    }

    /// <summary>
    /// Returns the UserId string representation.
    /// </summary>
    public override string ToString() => Value.ToString();

    /// <summary>
    /// Value equality comparison - two UserIds are equal if their values are the same.
    /// </summary>
    public override bool Equals(object? obj) => Equals(obj as UserId);

    /// <summary>
    /// Value equality comparison for UserId type.
    /// </summary>
    public bool Equals(UserId? other) => other is not null && Value == other.Value;

    /// <summary>
    /// Hash code based on UserId value for use in collections.
    /// </summary>
    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(UserId? left, UserId? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(UserId? left, UserId? right) => !(left == right);
}
