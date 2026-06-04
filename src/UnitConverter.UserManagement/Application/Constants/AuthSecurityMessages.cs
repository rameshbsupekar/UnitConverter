namespace UnitConverter.UserManagement.Application.Constants;

/// <summary>
/// Auth security and token error messages (non-validation).
/// </summary>
public static class AuthSecurityMessages
{
    public const string InvalidEmailOrPassword = "Invalid email or password.";
    public const string InvalidOrExpiredRefreshToken = "Invalid or expired refresh token.";
    public const string UserNotAvailable = "User is not available.";
    public const string UserAlreadyExistsFormat = "User with email {0} already exists";
}
