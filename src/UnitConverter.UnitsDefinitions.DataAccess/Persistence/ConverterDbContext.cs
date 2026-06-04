using Microsoft.EntityFrameworkCore;
using UnitConverter.UnitsDefinitions.DataAccess.Persistence.Entities;

namespace UnitConverter.UnitsDefinitions.DataAccess.Persistence;

/// <summary>
/// EF Core database context for the conversion catalog (data access layer).
/// </summary>
public sealed class ConverterDbContext : DbContext
{
    public ConverterDbContext(DbContextOptions<ConverterDbContext> options)
        : base(options)
    {
    }

    public DbSet<UnitDimensionEntity> UnitDimensions => Set<UnitDimensionEntity>();

    public DbSet<UnitEntity> Units => Set<UnitEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ConverterDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
