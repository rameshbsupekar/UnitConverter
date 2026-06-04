namespace UnitConverter.UnitsDefinitions.Contracts.Responses;

/// <summary>
/// Wire response for a unit conversion operation (converted value in the target unit).
/// </summary>
public sealed record ConversionResponse(
    decimal Value,
    string Symbol);
