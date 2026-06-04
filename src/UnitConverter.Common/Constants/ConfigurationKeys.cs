namespace UnitConverter.Common.Constants;

/// <summary>
/// Configuration section and key names (appsettings, environment variables).
/// </summary>
public static class ConfigurationKeys
{
    public static class ConnectionStrings
    {
        public const string Section = "ConnectionStrings";
        public const string UserManagement = "UserManagement";
        public const string UnitsMasterData = "UnitsMasterData";
    }

    public static class Jwt
    {
        public const string Section = "Jwt";
        public const string Secret = $"{Section}:Secret";
        public const string Issuer = $"{Section}:Issuer";
        public const string Audience = $"{Section}:Audience";
    }
}
