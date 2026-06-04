using Microsoft.EntityFrameworkCore;
using UnitConverter.UserManagement.DataAccess.Data;
using UnitConverter.Common.Constants;
using UnitConverter.UnitsDefinitions.DataAccess.Persistence;

var applySeed = args.Contains("--seed", StringComparer.OrdinalIgnoreCase)
    || !args.Contains("--migrate-only", StringComparer.OrdinalIgnoreCase);

DatabasePaths.EnsureSqliteDirectoriesExist();

Console.WriteLine("Applying EF Core migrations...");
await ApplyAuthMigrationsAsync();
await ApplyUnitsMigrationsAsync();
Console.WriteLine("Migrations applied.");

if (applySeed)
{
    Console.WriteLine("Seeding development reference data...");
    await SeedAuthAsync();
    await SeedUnitsAsync();
    Console.WriteLine("Seed complete.");
}

Console.WriteLine();
Console.WriteLine("Database ready:");
Console.WriteLine($"  {DatabasePaths.UserManagementDatabasePath}");
Console.WriteLine($"  {DatabasePaths.UnitsMasterDatabasePath}");

static async Task ApplyAuthMigrationsAsync()
{
    var options = new DbContextOptionsBuilder<AuthDbContext>()
        .UseSqlite(DatabasePaths.UserManagementConnectionString)
        .Options;

    await using var db = new AuthDbContext(options);
    await db.Database.MigrateAsync();
}

static async Task ApplyUnitsMigrationsAsync()
{
    var options = new DbContextOptionsBuilder<ConverterDbContext>()
        .UseSqlite(DatabasePaths.UnitsMasterDataConnectionString)
        .Options;

    await using var db = new ConverterDbContext(options);
    await db.Database.MigrateAsync();
}

static async Task SeedAuthAsync()
{
    var options = new DbContextOptionsBuilder<AuthDbContext>()
        .UseSqlite(DatabasePaths.UserManagementConnectionString)
        .Options;

    await using var db = new AuthDbContext(options);
    await UserManagementSeedData.SeedAsync(db);
}

static async Task SeedUnitsAsync()
{
    var options = new DbContextOptionsBuilder<ConverterDbContext>()
        .UseSqlite(DatabasePaths.UnitsMasterDataConnectionString)
        .Options;

    await using var db = new ConverterDbContext(options);
    await UnitCatalogSeedData.SeedAsync(db);
}
