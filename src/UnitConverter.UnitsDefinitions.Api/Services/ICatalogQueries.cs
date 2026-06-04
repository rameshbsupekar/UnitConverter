using UnitConverter.Common.Contracts.Paging;
using UnitConverter.UnitsDefinitions.Contracts.Responses;

namespace UnitConverter.UnitsDefinitions.Api.Services;

/// <summary>
/// Read-only catalog queries for public categories and approved units.
/// </summary>
public interface ICatalogQueries
{
    /// <summary>Returns all measurement categories exposed by the catalog.</summary>
    Task<IReadOnlyList<UnitCategoryResponse>> GetCategoriesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Returns a paged list of approved units for the given category query.</summary>
    Task<PagedResult<UnitCatalogItemResponse>> GetUnitsByCategoryAsync(
        GetUnitsByCategoryQuery query,
        PagedRequest paging,
        CancellationToken cancellationToken = default);
}
