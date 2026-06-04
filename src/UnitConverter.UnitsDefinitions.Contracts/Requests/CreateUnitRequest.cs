using UnitConverter.UnitsDefinitions.Contracts.Units;

namespace UnitConverter.UnitsDefinitions.Contracts.Requests;

/// <summary>
/// Admin request to add a unit to the catalog.
/// </summary>
public sealed record CreateUnitRequest(
    string Symbol,
    string DisplayName,
    UnitCategory Category,
    decimal MultiplierToBase,
    decimal OffsetToBase = 0m);
