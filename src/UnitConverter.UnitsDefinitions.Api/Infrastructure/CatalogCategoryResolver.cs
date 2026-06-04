using UnitConverter.UnitsDefinitions.Api.Constants;
using UnitConverter.UnitsDefinitions.Contracts.Units;

namespace UnitConverter.UnitsDefinitions.Api.Infrastructure;

/// <summary>
/// Resolves a <see cref="UnitCategory"/> from API query parameters (numeric id or wire name).
/// </summary>
internal static class CatalogCategoryResolver
{
    public static bool TryResolve(
        UnitCategory? categoryId,
        string? categoryName,
        out UnitCategory category,
        out string? errorMessage)
    {
        category = default;
        errorMessage = null;

        if (categoryId.HasValue && !string.IsNullOrWhiteSpace(categoryName))
        {
            if (!UnitCategoryExtensions.TryParseWireName(categoryName, out var fromName))
            {
                errorMessage = string.Format(
                    CatalogResolverMessages.UnknownCategoryNameFormat,
                    categoryName);
                return false;
            }

            if (fromName != categoryId.Value)
            {
                errorMessage = string.Format(
                    CatalogResolverMessages.CategoryParametersMismatchFormat,
                    (int)categoryId.Value,
                    categoryName);
                return false;
            }

            category = categoryId.Value;
            return true;
        }

        if (categoryId.HasValue)
        {
            if (!Enum.IsDefined(categoryId.Value))
            {
                errorMessage = string.Format(
                    CatalogResolverMessages.UnknownCategoryIdFormat,
                    (int)categoryId.Value);
                return false;
            }

            category = categoryId.Value;
            return true;
        }

        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            if (UnitCategoryExtensions.TryParseWireName(categoryName, out category))
            {
                return true;
            }

            errorMessage = string.Format(
                CatalogResolverMessages.UnknownCategoryNameWithWeightAliasFormat,
                categoryName);
            return false;
        }

        errorMessage = CatalogResolverMessages.CategoryParameterRequired;
        return false;
    }
}
