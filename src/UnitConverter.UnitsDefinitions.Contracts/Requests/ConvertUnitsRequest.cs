using UnitConverter.UnitsDefinitions.Contracts.Units;

namespace UnitConverter.UnitsDefinitions.Contracts.Requests;

/// <summary>
/// Wire request for a unit conversion operation.
/// </summary>
public sealed record ConvertUnitsRequest(
    decimal Value,
    string FromUnit,
    string ToUnit,
    UnitCategory? Category = null);
