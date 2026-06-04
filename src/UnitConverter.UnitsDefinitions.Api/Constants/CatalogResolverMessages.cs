using UnitConverter.Common.Constants;

namespace UnitConverter.UnitsDefinitions.Api.Constants;

/// <summary>
/// Validation messages for catalog category query resolution.
/// </summary>
public static class CatalogResolverMessages
{
    private const string CategoriesHint =
        $"Use GET {ApiV1Paths.CatalogCategories} for valid names (e.g. length, mass, temperature).";

    public const string UnknownCategoryNameFormat =
        "Unknown category name '{0}'. " + CategoriesHint;

    public const string UnknownCategoryNameWithWeightAliasFormat =
        "Unknown category name '{0}'. " + CategoriesHint + " (legacy alias: weight).";

    public const string UnknownCategoryIdFormat =
        "Unknown category id '{0}'. Use GET " + ApiV1Paths.CatalogCategories + " for valid ids.";

    public const string CategoryParametersMismatchFormat =
        "Query parameters 'category' ({0}) and 'categoryName' ('{1}') do not match.";

    public const string CategoryParameterRequired =
        "Specify either 'category' (id from GET " + ApiV1Paths.CatalogCategories
        + ") or 'categoryName' (e.g. length, mass, temperature).";
}
