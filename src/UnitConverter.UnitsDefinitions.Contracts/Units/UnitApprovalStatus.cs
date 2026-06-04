namespace UnitConverter.UnitsDefinitions.Contracts.Units;

/// <summary>
/// Master-data approval lifecycle for <c>Unit</c> rows.
/// </summary>
public enum UnitApprovalStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Rejected = 3
}
