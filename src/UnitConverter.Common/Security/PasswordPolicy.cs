using System.Text.RegularExpressions;
using UnitConverter.Common.Constants.Validation;

namespace UnitConverter.Common.Security;

/// <summary>
/// Password length and complexity rules (shared validation).
/// </summary>
public static class PasswordPolicy
{
    public static readonly string RequirementsMessage =
        "Password must be at least 12 characters and contain uppercase, lowercase, digit, and special character.";

    public static bool IsValid(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        if (password.Length < FieldLengthLimits.PasswordMinLength ||
            password.Length > FieldLengthLimits.PasswordMaxLength)
        {
            return false;
        }

        if (!Regex.IsMatch(password, "[A-Z]") ||
            !Regex.IsMatch(password, "[a-z]") ||
            !Regex.IsMatch(password, "[0-9]") ||
            !Regex.IsMatch(password, @"[^a-zA-Z0-9]"))
        {
            return false;
        }

        return true;
    }
}
