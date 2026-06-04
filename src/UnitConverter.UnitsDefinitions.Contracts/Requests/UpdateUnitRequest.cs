namespace UnitConverter.UnitsDefinitions.Contracts.Requests;

/// <summary>
/// Admin request to update unit master data (symbol is immutable).
/// </summary>
public sealed record UpdateUnitRequest(
    string DisplayName,
    decimal MultiplierToBase,
    decimal OffsetToBase = 0m);
