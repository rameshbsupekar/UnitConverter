using UnitConverter.UnitsDefinitions.Contracts.Units;

namespace UnitConverter.UnitsDefinitions.DataAccess.Persistence.Entities;

/// <summary>
/// Unit definition with affine conversion to the dimension base unit.
/// </summary>
public sealed class UnitEntity
{
    public int UnitId { get; set; }

    public int DimensionId { get; set; }

    public UnitDimensionEntity? Dimension { get; set; }

    public required string Name { get; set; }

    public required string Symbol { get; set; }

    public bool IsBaseUnit { get; set; }

    public decimal MultiplierToBase { get; set; }

    public decimal OffsetToBase { get; set; }

    public UnitApprovalStatus ApprovalStatus { get; set; }

    public string? SubmittedBy { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? RejectionReason { get; set; }

    public DateTime CreatedDate { get; set; }

    public required string CreatedBy { get; set; }

    public DateTime ModifiedDate { get; set; }

    public required string ModifiedBy { get; set; }
}
