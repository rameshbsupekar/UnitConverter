using Microsoft.EntityFrameworkCore;
using UnitConverter.Common.Contracts.Paging;
using UnitConverter.UnitsDefinitions.Catalog;
using UnitConverter.UnitsDefinitions.Contracts.Units;
using UnitConverter.UnitsDefinitions.Units;
using UnitConverter.UnitsDefinitions.DataAccess.Persistence;
using UnitConverter.UnitsDefinitions.DataAccess.Persistence.Entities;

namespace UnitConverter.UnitsDefinitions.DataAccess.Repositories;

/// <inheritdoc />
public sealed class UnitCatalogRepository : IUnitCatalogRepository
{
    private readonly ConverterDbContext _db;

    public UnitCatalogRepository(ConverterDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<UnitDefinition?> FindBySymbolAsync(
        string symbol,
        UnitCategory category,
        CancellationToken cancellationToken = default)
    {
        var normalized = symbol.Trim().ToLowerInvariant();
        var dimensionId = (int)category;
        var entity = await ApprovedUnitsQuery(dimensionId)
            .FirstOrDefaultAsync(u => u.Symbol == normalized, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IReadOnlyList<UnitDefinition>> GetByCategoryAsync(
        UnitCategory category,
        CancellationToken cancellationToken = default)
    {
        var entities = await ApprovedUnitsOrderedQuery((int)category)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain).ToList();
    }

    /// <inheritdoc />
    public async Task<PagedQueryResult<UnitDefinition>> GetApprovedByCategoryPagedAsync(
        UnitCategory category,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (entities, totalCount) = await QueryablePaging.ToPageAsync(
            ApprovedUnitsOrderedQuery((int)category),
            page,
            pageSize,
            cancellationToken);

        IReadOnlyList<UnitDefinition> items = entities.Select(ToDomain).ToList();
        return new PagedQueryResult<UnitDefinition>(items, totalCount);
    }

    private IQueryable<UnitEntity> ApprovedUnitsQuery(int dimensionId) =>
        _db.Units.AsNoTracking()
            .Where(u => u.DimensionId == dimensionId
                        && u.ApprovalStatus == UnitApprovalStatus.Approved);

    /// <summary>Stable sort: symbol then unique id (see EF pagination guidance).</summary>
    private IQueryable<UnitEntity> ApprovedUnitsOrderedQuery(int dimensionId) =>
        ApprovedUnitsQuery(dimensionId)
            .OrderBy(u => u.Symbol)
            .ThenBy(u => u.UnitId);

    private static UnitDefinition ToDomain(UnitEntity entity) =>
        new(
            entity.Symbol,
            entity.Name,
            (UnitCategory)entity.DimensionId,
            entity.MultiplierToBase,
            entity.OffsetToBase);
}
