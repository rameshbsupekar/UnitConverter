using UnitConverter.UnitsDefinitions.Contracts.Units;

namespace UnitConverter.UnitsDefinitions.Contracts.Responses;

/// <summary>
/// Unit master data returned by catalog/admin APIs.
/// </summary>
public sealed record UnitDetailResponse(
    int Id,
    string Symbol,
    string DisplayName,
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
