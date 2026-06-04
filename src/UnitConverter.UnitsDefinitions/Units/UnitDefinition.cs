using UnitConverter.UnitsDefinitions.Contracts.Units;

namespace UnitConverter.UnitsDefinitions.Units;

/// <summary>
/// A measurable unit within a dimension (domain entity — no EF/HTTP dependencies).
/// </summary>
public sealed class UnitDefinition
{
    public UnitDefinition(
        string symbol,
        string name,
        UnitCategory category,
        decimal multiplierToBase,
        decimal offsetToBase = 0m)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("Symbol is required.", nameof(symbol));
        }

        if (multiplierToBase <= 0)
        {
            throw new ArgumentException("MultiplierToBase must be greater than zero.", nameof(multiplierToBase));
        }

        Symbol = symbol.Trim().ToLowerInvariant();
        Name = string.IsNullOrWhiteSpace(name) ? symbol : name.Trim();
        Category = category;
        MultiplierToBase = multiplierToBase;
        OffsetToBase = offsetToBase;
    }

    public string Symbol { get; }

    /// <summary>Human-readable unit name (e.g. Meter, Celsius).</summary>
    public string Name { get; }

    public UnitCategory Category { get; }

    /// <summary>Affine conversion to the dimension base unit: base = value × MultiplierToBase + OffsetToBase.</summary>
    public decimal MultiplierToBase { get; }

    public decimal OffsetToBase { get; }
}
