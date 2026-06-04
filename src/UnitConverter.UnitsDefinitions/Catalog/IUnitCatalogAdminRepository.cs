using UnitConverter.Common.Contracts.Paging;
using UnitConverter.UnitsDefinitions.Contracts.Units;
using UnitConverter.UnitsDefinitions.Units;

namespace UnitConverter.UnitsDefinitions.Catalog;

/// <summary>
/// Port for admin CRUD on unit master data.
/// </summary>
public interface IUnitCatalogAdminRepository
{
    /// <summary>Gets a catalog entry by primary key.</summary>
    Task<CatalogUnit?> GetEntryByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Gets a domain unit by symbol and category.</summary>
    Task<UnitDefinition?> GetBySymbolAsync(string symbol, UnitCategory category, CancellationToken cancellationToken = default);

    /// <summary>Checks whether a symbol already exists in a category.</summary>
    Task<bool> ExistsBySymbolAsync(
        string symbol,
        UnitCategory category,
        int? excludeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Lists catalog entries with stable ordering and database paging.</summary>
    Task<PagedQueryResult<CatalogUnit>> ListEntriesPagedAsync(
        UnitCategory? category,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Persists a new unit and returns its generated id.</summary>
    Task<int> AddAsync(
        UnitDefinition unit,
        string actorEmail,
        UnitApprovalStatus initialStatus,
        CancellationToken cancellationToken = default);

    /// <summary>Updates mutable fields; may reset approval when <paramref name="requiresReapproval"/> is true.</summary>
    Task UpdateAsync(
        int id,
        string name,
        decimal multiplierToBase,
        decimal offsetToBase,
        string modifiedBy,
        bool requiresReapproval,
        CancellationToken cancellationToken = default);

    /// <summary>Marks a unit as approved.</summary>
    Task ApproveAsync(int id, string approvedBy, CancellationToken cancellationToken = default);

    /// <summary>Marks a unit as rejected with an optional reason.</summary>
    Task RejectAsync(int id, string rejectedBy, string? reason, CancellationToken cancellationToken = default);

    /// <summary>Removes a unit definition.</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
