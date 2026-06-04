namespace UnitConverter.UnitsDefinitions.DataAccess.Persistence.Entities;

/// <summary>
/// Measurement dimension (length, mass, temperature).
/// </summary>
public sealed class UnitDimensionEntity
{
    public int DimensionId { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public ICollection<UnitEntity> Units { get; set; } = [];
}
