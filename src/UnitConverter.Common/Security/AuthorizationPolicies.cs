namespace UnitConverter.Common.Security;

/// <summary>
/// Authorization policy names shared across API hosts.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>Admin, Employee, or Partner — read, create, and update unit master data.</summary>
    public const string MasterDataCrud = "MasterDataCrud";

    /// <summary>Admin only — delete unit master data.</summary>
    public const string MasterDataAdminDelete = "MasterDataAdminDelete";

    /// <summary>Admin only — verify or override master data changes.</summary>
    public const string MasterDataAdminOverride = "MasterDataAdminOverride";

    /// <summary>Admin only — approve or reject pending unit definitions.</summary>
    public const string MasterDataAdminApprove = "MasterDataAdminApprove";
}
