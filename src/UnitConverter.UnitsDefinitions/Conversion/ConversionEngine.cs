using UnitConverter.UnitsDefinitions.Contracts.Units;
using UnitConverter.UnitsDefinitions.Units;

namespace UnitConverter.UnitsDefinitions.Conversion;

/// <inheritdoc />
public sealed class ConversionEngine : IConversionEngine
{
    /// <inheritdoc />
    public decimal Convert(
        decimal value,
        string fromUnit,
        string toUnit,
        UnitCategory category,
        IReadOnlyDictionary<string, UnitDefinition> unitsInCategory)
    {
        ArgumentNullException.ThrowIfNull(unitsInCategory);

        var from = Normalize(fromUnit);
        var to = Normalize(toUnit);

        if (from == to)
        {
            return value;
        }

        _ = category;

        if (!unitsInCategory.TryGetValue(from, out var fromDef))
        {
            throw new ConversionException(string.Format(ConversionMessages.UnknownSourceUnitFormat, from));
        }

        if (!unitsInCategory.TryGetValue(to, out var toDef))
        {
            throw new ConversionException(string.Format(ConversionMessages.UnknownTargetUnitFormat, to));
        }

        if (fromDef.MultiplierToBase <= 0 || toDef.MultiplierToBase <= 0)
        {
            throw new ConversionException(ConversionMessages.InvalidMultipliers);
        }

        var baseValue = value * fromDef.MultiplierToBase + fromDef.OffsetToBase;
        return (baseValue - toDef.OffsetToBase) / toDef.MultiplierToBase;
    }

    private static string Normalize(string unit) =>
        unit.Trim().ToLowerInvariant();
}
