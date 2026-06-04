using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UnitConverter.UnitsDefinitions.DataAccess.Persistence.Entities;

namespace UnitConverter.UnitsDefinitions.DataAccess.Persistence.Configurations;

internal sealed class UnitConfiguration : IEntityTypeConfiguration<UnitEntity>
{
    public void Configure(EntityTypeBuilder<UnitEntity> builder)
    {
        builder.ToTable("Unit", t =>
        {
            t.HasCheckConstraint("CK_Unit_MultiplierToBase_Positive", "MultiplierToBase > 0");
            t.HasCheckConstraint(
                "CK_Unit_ApprovalStatus",
                "ApprovalStatus >= 0 AND ApprovalStatus <= 3");
        });

        builder.HasKey(x => x.UnitId);

        builder.Property(x => x.UnitId)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.DimensionId)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Symbol)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.MultiplierToBase)
            .HasPrecision(38, 18)
            .HasDefaultValue(1m)
            .IsRequired();

        builder.Property(x => x.OffsetToBase)
            .HasPrecision(38, 18)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.IsBaseUnit)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.ApprovalStatus)
            .IsRequired();

        builder.Property(x => x.SubmittedBy)
            .HasMaxLength(254);

        builder.Property(x => x.SubmittedAt)
            .HasPrecision(6);

        builder.Property(x => x.ApprovedBy)
            .HasMaxLength(254);

        builder.Property(x => x.ApprovedAt)
            .HasPrecision(6);

        builder.Property(x => x.RejectionReason)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedDate)
            .HasPrecision(6)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(254)
            .IsRequired();

        builder.Property(x => x.ModifiedDate)
            .HasPrecision(6)
            .IsRequired();

        builder.Property(x => x.ModifiedBy)
            .HasMaxLength(254)
            .IsRequired();

        builder.HasOne(x => x.Dimension)
            .WithMany(d => d.Units)
            .HasForeignKey(x => x.DimensionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Unit_Dimension");

        builder.HasIndex(x => new { x.DimensionId, x.Symbol })
            .IsUnique()
            .HasDatabaseName("UQ_Unit_Dimension_Symbol");

        builder.HasIndex(x => x.DimensionId)
            .HasDatabaseName("IX_Unit_DimensionId");

        builder.HasIndex(x => x.ApprovalStatus)
            .HasDatabaseName("IX_Unit_ApprovalStatus");

        builder.HasIndex(x => new { x.DimensionId, x.ApprovalStatus })
            .HasDatabaseName("IX_Unit_DimensionId_ApprovalStatus");
    }
}
