namespace UnitConverter.UnitsDefinitions.Contracts.Units;

/// <summary>
/// Supported measurement dimensions for unit conversion and catalog APIs.
/// Values align with <c>UnitDimension.DimensionId</c> in the catalog database.
/// </summary>
public enum UnitCategory
{
    Length = 1,
    Mass = 2,
    Temperature = 3
}
