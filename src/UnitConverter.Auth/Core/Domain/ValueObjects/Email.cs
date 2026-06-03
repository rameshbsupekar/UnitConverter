using System;
using System.Text.RegularExpressions;
using UnitConverter.Auth.Core.Domain.Exceptions;

namespace UnitConverter.Auth.Core.Domain.ValueObjects;

/// <summary>
/// Email value object - strongly typed, immutable, validates on creation.
/// RFC 5322 basic validation (simplified, not full spec).
/// Max length: 254 characters (RFC 5321).
/// </summary>
public sealed class Email : IEquatable<Email>
{
    /// <summary>
    /// Maximum allowed length for email address (per RFC 5321).
    /// </summary>
    private const int MaxEmailLength = 254;

    /// <summary>
    /// Simple regex pattern for basic email validation (RFC 5322 simplified).
    /// Pattern: localpart@domain.extension
    /// Supports: alphanumeric, dots, hyphens, underscores in local part.
    /// </summary>
    private static readonly Regex EmailPattern = new(
        @"^[a-zA-Z0-9._\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled);

    /// <summary>
    /// The email address value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Private constructor - instances created via static factory.
    /// </summary>
    private Email(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Static factory method to create Email instance with validation.
    /// </summary>
    /// <param name="email">Email address string</param>
    /// <returns>Email value object instance</returns>
    /// <exception cref="InvalidEmailException">Thrown if email validation fails</exception>
    public static Email Create(string email)
    {
        // Validation 1: Check for null or empty
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidEmailException("Email cannot be empty or whitespace.");
        }

        var trimmedEmail = email.Trim();

        // Validation 2: Check length
        if (trimmedEmail.Length > MaxEmailLength)
        {
            throw new InvalidEmailException(
                $"Email length cannot exceed {MaxEmailLength} characters. Provided: {trimmedEmail.Length}");
        }

        // Validation 3: Check format (RFC 5322 basic)
        if (!EmailPattern.IsMatch(trimmedEmail))
        {
            throw new InvalidEmailException(
                $"Email format is invalid. Expected format: localpart@domain.extension. Provided: {trimmedEmail}");
        }

        return new Email(trimmedEmail);
    }

    /// <summary>
    /// Returns the email string representation.
    /// </summary>
    public override string ToString() => Value;

    /// <summary>
    /// Value equality comparison - two emails are equal if their values are the same.
    /// </summary>
    public override bool Equals(object? obj) => Equals(obj as Email);

    /// <summary>
    /// Value equality comparison for Email type.
    /// </summary>
    public bool Equals(Email? other) => other is not null && Value == other.Value;

    /// <summary>
    /// Hash code based on email value for use in collections.
    /// </summary>
    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(Email? left, Email? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(Email? left, Email? right) => !(left == right);
}
