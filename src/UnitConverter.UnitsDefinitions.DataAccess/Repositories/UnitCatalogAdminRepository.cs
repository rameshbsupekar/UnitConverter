using Microsoft.EntityFrameworkCore;
using UnitConverter.Common.Contracts.Paging;
using UnitConverter.UnitsDefinitions.Catalog;
using UnitConverter.UnitsDefinitions.Contracts.Units;
using UnitConverter.UnitsDefinitions.Units;
using UnitConverter.UnitsDefinitions.DataAccess.Persistence;
using UnitConverter.UnitsDefinitions.DataAccess.Persistence.Entities;

namespace UnitConverter.UnitsDefinitions.DataAccess.Repositories;

/// <inheritdoc />
public sealed class UnitCatalogAdminRepository : IUnitCatalogAdminRepository
{
    private readonly ConverterDbContext _db;

    public UnitCatalogAdminRepository(ConverterDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<CatalogUnit?> GetEntryByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Units
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UnitId == id, cancellationToken);

        return entity is null ? null : ToCatalogUnit(entity);
    }

    /// <inheritdoc />
    public async Task<UnitDefinition?> GetBySymbolAsync(
        string symbol,
        UnitCategory category,
        CancellationToken cancellationToken = default)
    {
        var normalized = symbol.Trim().ToLowerInvariant();
        var dimensionId = (int)category;
        var entity = await _db.Units
            .AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.Symbol == normalized && u.DimensionId == dimensionId,
                cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    /// <inheritdoc />
    public Task<bool> ExistsBySymbolAsync(
        string symbol,
        UnitCategory category,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = symbol.Trim().ToLowerInvariant();
        var dimensionId = (int)category;
        var query = _db.Units.Where(u => u.Symbol == normalized && u.DimensionId == dimensionId);
        if (excludeId.HasValue)
        {
            query = query.Where(u => u.UnitId != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PagedQueryResult<CatalogUnit>> ListEntriesPagedAsync(
        UnitCategory? category,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (entities, totalCount) = await QueryablePaging.ToPageAsync(
            ListEntriesOrderedQuery(category),
            page,
            pageSize,
            cancellationToken);

        IReadOnlyList<CatalogUnit> items = entities.Select(ToCatalogUnit).ToList();
        return new PagedQueryResult<CatalogUnit>(items, totalCount);
    }

    private IQueryable<UnitEntity> ListEntriesOrderedQuery(UnitCategory? category)
    {
        var query = _db.Units.AsNoTracking();
        if (category.HasValue)
        {
            query = query.Where(u => u.DimensionId == (int)category.Value);
        }

        return query
            .OrderBy(u => u.DimensionId)
            .ThenBy(u => u.Symbol)
            .ThenBy(u => u.UnitId);
    }

    /// <inheritdoc />
    public async Task<int> AddAsync(
        UnitDefinition unit,
        string actorEmail,
        UnitApprovalStatus initialStatus,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var isApproved = initialStatus == UnitApprovalStatus.Approved;

        var entity = new UnitEntity
        {
            DimensionId = (int)unit.Category,
            Symbol = unit.Symbol,
            Name = unit.Name,
            MultiplierToBase = unit.MultiplierToBase,
            OffsetToBase = unit.OffsetToBase,
            IsBaseUnit = false,
            ApprovalStatus = initialStatus,
            SubmittedBy = actorEmail,
            SubmittedAt = now,
            ApprovedBy = isApproved ? actorEmail : null,
            ApprovedAt = isApproved ? now : null,
            CreatedDate = now,
            CreatedBy = actorEmail,
            ModifiedDate = now,
            ModifiedBy = actorEmail
        };

        _db.Units.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity.UnitId;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(
        int id,
        string name,
        decimal multiplierToBase,
        decimal offsetToBase,
        string modifiedBy,
        bool requiresReapproval,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.Units.FirstOrDefaultAsync(u => u.UnitId == id, cancellationToken)
            ?? throw UnitNotFoundException.ForId(id);

        var now = DateTime.UtcNow;
        entity.Name = name;
        entity.MultiplierToBase = multiplierToBase;
        entity.OffsetToBase = offsetToBase;
        entity.ModifiedDate = now;
        entity.ModifiedBy = modifiedBy;

        if (requiresReapproval && entity.ApprovalStatus == UnitApprovalStatus.Approved)
        {
            entity.ApprovalStatus = UnitApprovalStatus.PendingApproval;
            entity.SubmittedBy = modifiedBy;
            entity.SubmittedAt = now;
            entity.ApprovedBy = null;
            entity.ApprovedAt = null;
            entity.RejectionReason = null;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ApproveAsync(int id, string approvedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Units.FirstOrDefaultAsync(u => u.UnitId == id, cancellationToken)
            ?? throw UnitNotFoundException.ForId(id);

        if (entity.ApprovalStatus is not (UnitApprovalStatus.PendingApproval or UnitApprovalStatus.Rejected))
        {
            throw new UnitCatalogException(
                string.Format(UnitCatalogMessages.CannotApproveFormat, id, entity.ApprovalStatus));
        }

        var now = DateTime.UtcNow;
        entity.ApprovalStatus = UnitApprovalStatus.Approved;
        entity.ApprovedBy = approvedBy;
        entity.ApprovedAt = now;
        entity.RejectionReason = null;
        entity.ModifiedBy = approvedBy;
        entity.ModifiedDate = now;

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RejectAsync(
        int id,
        string rejectedBy,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.Units.FirstOrDefaultAsync(u => u.UnitId == id, cancellationToken)
            ?? throw UnitNotFoundException.ForId(id);

        if (entity.ApprovalStatus != UnitApprovalStatus.PendingApproval)
        {
            throw new UnitCatalogException(
                string.Format(UnitCatalogMessages.CannotRejectFormat, id, entity.ApprovalStatus));
        }

        var now = DateTime.UtcNow;
        entity.ApprovalStatus = UnitApprovalStatus.Rejected;
        entity.RejectionReason = reason;
        entity.ApprovedBy = null;
        entity.ApprovedAt = null;
        entity.ModifiedBy = rejectedBy;
        entity.ModifiedDate = now;

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Units.FirstOrDefaultAsync(u => u.UnitId == id, cancellationToken);
        if (entity is null)
        {
            throw UnitNotFoundException.ForId(id);
        }

        _db.Units.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static UnitDefinition ToDomain(UnitEntity entity) =>
        new(
            entity.Symbol,
            entity.Name,
            (UnitCategory)entity.DimensionId,
            entity.MultiplierToBase,
            entity.OffsetToBase);

    private static CatalogUnit ToCatalogUnit(UnitEntity entity) =>
        new(
            entity.UnitId,
            entity.Symbol,
            entity.Name,
            (UnitCategory)entity.DimensionId,
            entity.MultiplierToBase,
            entity.OffsetToBase,
            entity.IsBaseUnit,
            entity.ApprovalStatus,
            entity.SubmittedBy,
            entity.SubmittedAt,
            entity.ApprovedBy,
            entity.ApprovedAt,
            entity.RejectionReason,
            entity.CreatedDate,
            entity.CreatedBy,
            entity.ModifiedDate,
            entity.ModifiedBy);
}
