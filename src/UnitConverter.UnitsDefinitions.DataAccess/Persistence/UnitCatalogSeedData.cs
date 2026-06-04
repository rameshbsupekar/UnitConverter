using Microsoft.EntityFrameworkCore;
using UnitConverter.UnitsDefinitions.Contracts.Units;
using UnitConverter.UnitsDefinitions.DataAccess.Persistence.Entities;

namespace UnitConverter.UnitsDefinitions.DataAccess.Persistence;

internal static class UnitCatalogSeedData
{
    internal const string SeedActor = "system-seed";

    private const decimal FahrenheitOffset = 255.372222222222222m;
    private const decimal FahrenheitMultiplier = 0.555555555555555556m;

    public static IReadOnlyList<UnitEntity> Create()
    {
        var now = DateTime.UtcNow;
        return
        [
            Unit(UnitCategory.Length, "Meter", "m", isBase: true, 1m, 0m, now),
            Unit(UnitCategory.Length, "Kilometer", "km", multiplier: 1000m, now: now),
            Unit(UnitCategory.Length, "Centimeter", "cm", multiplier: 0.01m, now: now),
            Unit(UnitCategory.Length, "Millimeter", "mm", multiplier: 0.001m, now: now),
            Unit(UnitCategory.Length, "Inch", "in", multiplier: 0.0254m, now: now),
            Unit(UnitCategory.Length, "Foot", "ft", multiplier: 0.3048m, now: now),
            Unit(UnitCategory.Length, "Yard", "yd", multiplier: 0.9144m, now: now),
            Unit(UnitCategory.Length, "Mile", "mi", multiplier: 1609.344m, now: now),
            Unit(UnitCategory.Mass, "Kilogram", "kg", isBase: true, 1m, 0m, now),
            Unit(UnitCategory.Mass, "Gram", "g", multiplier: 0.001m, now: now),
            Unit(UnitCategory.Mass, "Milligram", "mg", multiplier: 0.000001m, now: now),
            Unit(UnitCategory.Mass, "Metric Ton", "t", multiplier: 1000m, now: now),
            Unit(UnitCategory.Mass, "Pound", "lb", multiplier: 0.45359237m, now: now),
            Unit(UnitCategory.Mass, "Ounce", "oz", multiplier: 0.028349523125m, now: now),
            Unit(UnitCategory.Temperature, "Kelvin", "k", isBase: true, 1m, 0m, now),
            Unit(UnitCategory.Temperature, "Celsius", "c", multiplier: 1m, offset: 273.15m, now: now),
            Unit(UnitCategory.Temperature, "Fahrenheit", "f", multiplier: FahrenheitMultiplier, offset: FahrenheitOffset, now: now)
        ];
    }

    public static async Task SeedAsync(ConverterDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await db.UnitDimensions.AnyAsync(cancellationToken))
        {
            db.UnitDimensions.AddRange(UnitDimensionSeedData.Create());
            await db.SaveChangesAsync(cancellationToken);
        }

        if (await db.Units.AnyAsync(cancellationToken))
        {
            return;
        }

        db.Units.AddRange(Create());
        await db.SaveChangesAsync(cancellationToken);
    }

    private static UnitEntity Unit(
        UnitCategory dimension,
        string name,
        string symbol,
        bool isBase = false,
        decimal multiplier = 1m,
        decimal offset = 0m,
        DateTime? now = null)
    {
        var timestamp = now ?? DateTime.UtcNow;
        return new UnitEntity
        {
            DimensionId = (int)dimension,
            Name = name,
            Symbol = symbol.Trim().ToLowerInvariant(),
            IsBaseUnit = isBase,
            MultiplierToBase = multiplier,
            OffsetToBase = offset,
            ApprovalStatus = UnitApprovalStatus.Approved,
            SubmittedBy = SeedActor,
            SubmittedAt = timestamp,
            ApprovedBy = SeedActor,
            ApprovedAt = timestamp,
            CreatedDate = timestamp,
            CreatedBy = SeedActor,
            ModifiedDate = timestamp,
            ModifiedBy = SeedActor
        };
    }
}
