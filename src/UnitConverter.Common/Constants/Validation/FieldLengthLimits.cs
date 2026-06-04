namespace UnitConverter.Common.Constants.Validation;

/// <summary>
/// Field length limits for validation (registration, profile, etc.).
/// </summary>
public static class FieldLengthLimits
{
    public const int PasswordMinLength = 12;
    public const int PasswordMaxLength = 128;
    public const int FirstNameMaxLength = 100;
    public const int LastNameMaxLength = 100;
    public const int OrganizationNameMaxLength = 200;
    public const int ApiKeyMinLength = 32;
}
