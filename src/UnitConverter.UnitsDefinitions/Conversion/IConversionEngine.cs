using UnitConverter.UnitsDefinitions.Contracts.Units;

namespace UnitConverter.UnitsDefinitions.Conversion;

/// <summary>
/// Domain service: converts values between units using catalog metadata and category rules.
/// </summary>
public interface IConversionEngine
{
    decimal Convert(
        decimal value,
        string fromUnit,
        string toUnit,
        UnitCategory category,
        IReadOnlyDictionary<string, Units.UnitDefinition> unitsInCategory);
}
