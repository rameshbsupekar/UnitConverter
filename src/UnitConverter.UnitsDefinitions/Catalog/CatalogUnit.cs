using UnitConverter.UnitsDefinitions.Contracts.Units;

namespace UnitConverter.UnitsDefinitions.Catalog;

/// <summary>
/// Unit row for admin catalog APIs (includes persistence id, approval, and audit).
/// </summary>
public sealed record CatalogUnit(
    int Id,
    string Symbol,
    string Name,
    UnitCategory Category,
    decimal MultiplierToBase,
    decimal OffsetToBase,
    bool IsBaseUnit,
    UnitApprovalStatus ApprovalStatus,
    string? SubmittedBy,
    DateTime? SubmittedAt,
    string? ApprovedBy,
    DateTime? ApprovedAt,
    string? RejectionReason,
    DateTime CreatedDate,
    string CreatedBy,
    DateTime ModifiedDate,
    string ModifiedBy);
