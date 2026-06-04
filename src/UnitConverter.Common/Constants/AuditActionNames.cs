namespace UnitConverter.Common.Constants;

/// <summary>
/// Audit log action identifiers for auth endpoints.
/// </summary>
public static class AuditActionNames
{
    public const string UserRegistration = "USER_REGISTRATION";
    public const string UserLogin = "USER_LOGIN";
    public const string UserLogout = "USER_LOGOUT";
    public const string AuthAction = "AUTH_ACTION";
    public const string AnonymousUser = "ANONYMOUS";
}
