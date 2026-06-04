using UnitConverter.UnitsDefinitions.Contracts.Units;
using UnitConverter.UnitsDefinitions.DataAccess.Persistence.Entities;

namespace UnitConverter.UnitsDefinitions.DataAccess.Persistence;

internal static class UnitDimensionSeedData
{
    public static IReadOnlyList<UnitDimensionEntity> Create() =>
    [
        Dimension(UnitCategory.Length, "Length", "Length measurements"),
        Dimension(UnitCategory.Mass, "Mass", "Mass / weight measurements"),
        Dimension(UnitCategory.Temperature, "Temperature", "Temperature measurements")
    ];

    private static UnitDimensionEntity Dimension(UnitCategory category, string name, string description) =>
        new()
        {
            DimensionId = (int)category,
            Name = name,
            Description = description
        };
}
