using UnitConverter.Common.Contracts.Paging;
using UnitConverter.UnitsDefinitions.Contracts.Requests;
using UnitConverter.UnitsDefinitions.Contracts.Responses;
using UnitConverter.UnitsDefinitions.Contracts.Units;

namespace UnitConverter.UnitsDefinitions.Api.Services;

/// <summary>
/// Application service for unit definition master data and approval workflow.
/// </summary>
public interface IUnitAdminService
{
    /// <summary>Creates a unit definition (pending approval unless the caller is Admin).</summary>
    Task<UnitDetailResponse> CreateAsync(CreateUnitRequest request, CancellationToken cancellationToken = default);

    /// <summary>Updates display name and conversion factors for an existing unit.</summary>
    Task<UnitDetailResponse> UpdateAsync(int id, UpdateUnitRequest request, CancellationToken cancellationToken = default);

    /// <summary>Approves a pending unit definition (Admin only).</summary>
    Task<UnitDetailResponse> ApproveAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Rejects a pending unit definition with an optional reason (Admin only).</summary>
    Task<UnitDetailResponse> RejectAsync(int id, RejectUnitRequest request, CancellationToken cancellationToken = default);

    /// <summary>Deletes a unit definition (Admin only).</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Gets a single unit definition by id, or null if not found.</summary>
    Task<UnitDetailResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Lists unit definitions with optional category filter and paging.</summary>
    Task<PagedResult<UnitDetailResponse>> ListAsync(
        UnitCategory? category,
        PagedRequest paging,
        CancellationToken cancellationToken = default);
}
