using UnitConverter.Common.Contracts.Paging;
using UnitConverter.UnitsDefinitions.Contracts.Units;
using UnitConverter.UnitsDefinitions.Units;

namespace UnitConverter.UnitsDefinitions.Catalog;

/// <summary>
/// Port for reading the unit catalog (implemented by Infrastructure / ORM).
/// </summary>
public interface IUnitCatalogRepository
{
    /// <summary>Finds an approved unit by symbol within a category.</summary>
    Task<UnitDefinition?> FindBySymbolAsync(
        string symbol,
        UnitCategory category,
        CancellationToken cancellationToken = default);

    /// <summary>All approved units in a category (used by conversion engine).</summary>
    Task<IReadOnlyList<UnitDefinition>> GetByCategoryAsync(
        UnitCategory category,
        CancellationToken cancellationToken = default);

    /// <summary>Approved units in a category with database paging (public catalog).</summary>
    Task<PagedQueryResult<UnitDefinition>> GetApprovedByCategoryPagedAsync(
        UnitCategory category,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
