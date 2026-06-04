using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UnitConverter.UnitsDefinitions.DataAccess.Persistence.Entities;

namespace UnitConverter.UnitsDefinitions.DataAccess.Persistence.Configurations;

internal sealed class UnitDimensionConfiguration : IEntityTypeConfiguration<UnitDimensionEntity>
{
    public void Configure(EntityTypeBuilder<UnitDimensionEntity> builder)
    {
        builder.ToTable("UnitDimension");

        builder.HasKey(x => x.DimensionId);

        builder.Property(x => x.DimensionId)
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.HasIndex(x => x.Name)
            .IsUnique()
            .HasDatabaseName("UQ_UnitDimension_Name");
    }
}
