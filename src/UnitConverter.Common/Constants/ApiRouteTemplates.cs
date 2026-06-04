namespace UnitConverter.Common.Constants;

/// <summary>
/// Shared attribute-route templates for versioned API controllers.
/// </summary>
public static class ApiRouteTemplates
{
    /// <summary>Prefix for URL-segment versioning: <c>api/v{version}/...</c>.</summary>
    public const string VersionedApiPrefix = "api/v{version:apiVersion}";
}
