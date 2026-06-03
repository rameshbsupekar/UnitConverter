using System;
using System.Text.RegularExpressions;

namespace UnitConverter.Auth.Common.Constants;

/// <summary>
/// Security-related constants and validation rules.
/// Includes password requirements, API key constraints, and role definitions.
/// </summary>
public static class SecurityConstants
{
    /// <summary>
    /// Minimum password length. Default: 12 characters.
    /// </summary>
    public const int PasswordMinLength = 12;

    /// <summary>
    /// Maximum password length. Default: 128 characters.
    /// </summary>
    public const int PasswordMaxLength = 128;

    /// <summary>
    /// Minimum API key length. Default: 32 characters.
    /// </summary>
    public const int ApiKeyMinLength = 32;

    /// <summary>
    /// Role name for administrator. Has full system access.
    /// </summary>
    public const string AdminRole = "Admin";

    /// <summary>
    /// Role name for partner integrations. Can access partner-specific APIs.
    /// </summary>
    public const string PartnerRole = "Partner";

    /// <summary>
    /// Role name for employees. Can access internal tools and APIs.
    /// </summary>
    public const string EmployeeRole = "Employee";

    /// <summary>
    /// Role name for public/anonymous users. Limited access to public APIs.
    /// </summary>
    public const string PublicRole = "Public";

    /// <summary>
    /// User-friendly message explaining password requirements.
    /// </summary>
    public static readonly string PasswordRequirementsMessage =
        "Password must be at least 12 characters and contain uppercase, lowercase, digit, and special character.";

    /// <summary>
    /// Validates password complexity requirements.
    /// Password must:
    /// - Be at least PasswordMinLength characters
    /// - Be at most PasswordMaxLength characters
    /// - Contain at least one uppercase letter
    /// - Contain at least one lowercase letter
    /// - Contain at least one digit
    /// - Contain at least one special character
    /// </summary>
    /// <param name="password">The password to validate.</param>
    /// <returns>True if password meets all requirements; otherwise false.</returns>
    public static bool IsValidPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        if (password.Length < PasswordMinLength || password.Length > PasswordMaxLength)
            return false;

        // Check for uppercase letter
        if (!Regex.IsMatch(password, "[A-Z]"))
            return false;

        // Check for lowercase letter
        if (!Regex.IsMatch(password, "[a-z]"))
            return false;

        // Check for digit
        if (!Regex.IsMatch(password, "[0-9]"))
            return false;

        // Check for special character
        // Match any character that is not alphanumeric
        if (!Regex.IsMatch(password, @"[^a-zA-Z0-9]"))
            return false;

        return true;
    }
}
