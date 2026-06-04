namespace UnitConverter.Common.Constants;

/// <summary>
/// Named MVC endpoints for link generation and OpenAPI operation ids.
/// Must match <c>Name = ...</c> on controller actions.
/// </summary>
public static class ApiRouteNames
{
    public const string RegisterUser = nameof(RegisterUser);
    public const string CreateSession = nameof(CreateSession);
    public const string RefreshSession = nameof(RefreshSession);

    public const string ListUnitDefinitions = nameof(ListUnitDefinitions);
    public const string GetUnitDefinitionById = nameof(GetUnitDefinitionById);
    public const string CreateUnitDefinition = nameof(CreateUnitDefinition);
}
