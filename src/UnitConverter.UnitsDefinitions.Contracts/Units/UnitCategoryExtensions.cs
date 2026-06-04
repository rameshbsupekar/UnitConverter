namespace UnitConverter.UnitsDefinitions.Contracts.Units;

/// <summary>
/// Maps <see cref="UnitCategory"/> to canonical wire strings (JSON, routes, query params).
/// </summary>
public static class UnitCategoryExtensions
{
    public static readonly IReadOnlyDictionary<UnitCategory, string> WireNames =
        new Dictionary<UnitCategory, string>
        {
            [UnitCategory.Length] = UnitCategoryWireNames.Length,
            [UnitCategory.Mass] = UnitCategoryWireNames.Mass,
            [UnitCategory.Temperature] = UnitCategoryWireNames.Temperature
        };

    private static readonly IReadOnlyDictionary<string, UnitCategory> LegacyAliases =
        new Dictionary<string, UnitCategory>(StringComparer.OrdinalIgnoreCase)
        {
            ["weight"] = UnitCategory.Mass
        };

    public static string ToWireName(this UnitCategory category) => WireNames[category];

    public static bool TryParseWireName(string? value, out UnitCategory category)
    {
        var match = WireNames.FirstOrDefault(pair =>
            string.Equals(pair.Value, value, StringComparison.OrdinalIgnoreCase));
        if (match.Value is not null)
        {
            category = match.Key;
            return true;
        }

        if (value is not null && LegacyAliases.TryGetValue(value, out category))
        {
            return true;
        }

        category = default;
        return false;
    }

    public static bool IsKnownWireName(string? value) => TryParseWireName(value, out _);

    public static string ToDisplayName(this UnitCategory category) => category switch
    {
        UnitCategory.Length => "Length",
        UnitCategory.Mass => "Mass",
        UnitCategory.Temperature => "Temperature",
        _ => category.ToString()
    };
}

/// <summary>
/// Canonical lowercase wire strings for <see cref="UnitCategory"/> (not API-specific).
/// </summary>
public static class UnitCategoryWireNames
{
    public const string Length = "length";
    public const string Mass = "mass";
    public const string Temperature = "temperature";

    /// <summary>Legacy alias; use <see cref="Mass"/> for new clients.</summary>
    public const string Weight = "weight";
}
