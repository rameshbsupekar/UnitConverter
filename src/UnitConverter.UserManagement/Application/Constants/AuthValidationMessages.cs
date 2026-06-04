using UnitConverter.Common.Constants.Validation;

namespace UnitConverter.UserManagement.Application.Constants;

/// <summary>
/// FluentValidation messages for auth requests.
/// </summary>
public static class AuthValidationMessages
{
    public const string EmailRequired = "Email is required";
    public const string InvalidEmailFormat = "Invalid email format";
    public const string PasswordRequired = "Password is required";
    public const string FirstNameRequired = "First name is required";
    public const string LastNameRequired = "Last name is required";
    public const string OrganizationNameRequired = "Organization name is required";
    public const string IdempotencyKeyRequired = "Idempotency key is required";
    public const string RefreshTokenRequired = "Refresh token is required";
    public const string InvalidFirstNameCharacters = "First name contains invalid characters";
    public const string InvalidLastNameCharacters = "Last name contains invalid characters";

    public static string PasswordMinLength(int min) =>
        $"Password must be at least {min} characters";

    public static string PasswordMaxLength(int max) =>
        $"Password cannot exceed {max} characters";

    public static string NameLengthRange(string fieldName, int max) =>
        $"{fieldName} must be 1-{max} characters";

    public static string OrganizationNameLengthRange(int max) =>
        $"Organization name must be 1-{max} characters";
}
