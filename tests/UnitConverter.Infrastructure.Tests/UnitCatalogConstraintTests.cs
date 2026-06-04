using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UnitConverter.UnitsDefinitions.Contracts.Units;
using UnitConverter.UnitsDefinitions.DataAccess.Persistence;
using UnitConverter.UnitsDefinitions.DataAccess.Persistence.Entities;
using Xunit;

namespace UnitConverter.Infrastructure.Tests;

public sealed class UnitCatalogConstraintTests
{
    [Fact]
    public async Task DuplicateDimensionAndSymbol_ViolatesUniqueIndex()
    {
        await using var db = await CreateMigratedContextAsync();
        await UnitCatalogSeedData.SeedAsync(db);

        db.Units.Add(new UnitEntity
        {
            DimensionId = (int)UnitCategory.Length,
            Symbol = "m",
            Name = "Duplicate",
            MultiplierToBase = 1m,
            ApprovalStatus = UnitApprovalStatus.Approved,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "test",
            ModifiedDate = DateTime.UtcNow,
            ModifiedBy = "test"
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    private static async Task<ConverterDbContext> CreateMigratedContextAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ConverterDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new ConverterDbContext(options);
        await db.Database.MigrateAsync();
        return db;
    }
}
