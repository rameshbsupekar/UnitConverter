namespace UnitConverter.UnitsDefinitions.Contracts.Requests;

/// <summary>
/// Admin request to reject a pending unit definition.
/// </summary>
public sealed record RejectUnitRequest(string? Reason);
