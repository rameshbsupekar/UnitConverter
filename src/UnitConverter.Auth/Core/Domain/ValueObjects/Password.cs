using System;
using System.Linq;
using UnitConverter.Auth.Core.Domain.Exceptions;

namespace UnitConverter.Auth.Core.Domain.ValueObjects;

/// <summary>
/// Password value object - handles password validation rules.
/// NOTE: Passwords are NOT stored in domain models. Only password hashes are stored in User entities.
/// This class provides validation and strength checking for plaintext passwords during registration/reset.
/// </summary>
public static class Password
{
    /// <summary>
    /// Minimum password length required.
    /// </summary>
    private const int MinPasswordLength = 12;

    /// <summary>
    /// Validates password strength.
    /// Requirements:
    /// - Minimum 12 characters
    /// - At least one uppercase letter (A-Z)
    /// - At least one lowercase letter (a-z)
    /// - At least one digit (0-9)
    /// - At least one special character (!@#$%^&*)
    /// </summary>
    public static bool IsValid(string password)
    {
        // Check for null or empty
        if (string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        // Check minimum length
        if (password.Length < MinPasswordLength)
        {
            return false;
        }

        // Check for uppercase letter
        if (!password.Any(char.IsUpper))
        {
            return false;
        }

        // Check for lowercase letter
        if (!password.Any(char.IsLower))
        {
            return false;
        }

        // Check for digit
        if (!password.Any(char.IsDigit))
        {
            return false;
        }

        // Check for special character
        const string specialChars = "!@#$%^&*";
        if (!password.Any(c => specialChars.Contains(c)))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates password and throws exception if invalid.
    /// </summary>
    public static void Validate(string password)
    {
        if (!IsValid(password))
        {
            throw new WeakPasswordException(
                $"Password must be at least {MinPasswordLength} characters and contain uppercase, lowercase, digit, and special character (!@#$%^&*).");
        }
    }
}
