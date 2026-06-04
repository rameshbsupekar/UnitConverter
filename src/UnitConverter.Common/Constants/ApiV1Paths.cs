namespace UnitConverter.Common.Constants;

/// <summary>
/// Fully qualified <c>/api/v1/...</c> paths for clients, tests, and documentation cross-references.
/// </summary>
public static class ApiV1Paths
{
    private const string Root = "/api/v1";

    public const string RegisterUser = Root + "/" + ApiRouteSegments.Users;
    public static string UserById(long id) => $"{RegisterUser}/{id}";
    public const string CreateSession = Root + "/" + ApiRouteSegments.Sessions;
    public const string RefreshSession = Root + "/" + ApiRouteSegments.Sessions + "/" + ApiRouteSegments.SessionRefresh;

    public const string Health = Root + EndpointPaths.Health;

    public const string CatalogCategories = Root + ApiRouteSegments.CatalogCategoriesPath;
    public const string CatalogUnits = Root + ApiRouteSegments.Catalog + "/" + ApiRouteSegments.CatalogUnits;
    public const string UnitConversions = Root + ApiRouteSegments.UnitConversions;
    public const string UnitDefinitions = Root + ApiRouteSegments.UnitDefinitions;

    public static string UnitDefinitionById(int id) => $"{UnitDefinitions}/{id}";

    public static string UnitDefinitionApprove(int id) =>
        $"{UnitDefinitions}/{id}/{ApiRouteSegments.Approve}";

    public static string UnitDefinitionReject(int id) =>
        $"{UnitDefinitions}/{id}/{ApiRouteSegments.Reject}";

    public static string UnitDefinitionAdminCorrection(int id) =>
        $"{UnitDefinitions}/{id}/{ApiRouteSegments.AdminCorrection}";
}
