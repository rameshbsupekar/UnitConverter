namespace UnitConverter.Common.Security;

/// <summary>
/// Authorization role names.
/// </summary>
public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Partner = "Partner";
    public const string Employee = "Employee";
    public const string Public = "Public";

    public static readonly IReadOnlyCollection<string> All =
        new[] { Admin, Partner, Employee, Public };

    /// <summary>Roles that may read, create, or update unit master data (delete is Admin only).</summary>
    public static readonly IReadOnlyCollection<string> MasterDataCrud =
        new[] { Admin, Employee, Partner };
}

