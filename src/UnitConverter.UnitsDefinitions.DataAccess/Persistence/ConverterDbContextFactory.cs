using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using UnitConverter.Common.Constants;

namespace UnitConverter.UnitsDefinitions.DataAccess.Persistence;

/// <summary>
/// Design-time factory for <c>dotnet ef</c> migrations (local SQLite under <c>Data/</c>).
/// </summary>
public sealed class ConverterDbContextFactory : IDesignTimeDbContextFactory<ConverterDbContext>
{
    public ConverterDbContext CreateDbContext(string[] args)
    {
        DatabasePaths.EnsureSqliteDirectoriesExist();

        var options = new DbContextOptionsBuilder<ConverterDbContext>()
            .UseSqlite(DatabasePaths.UnitsMasterDataConnectionString)
            .Options;

        return new ConverterDbContext(options);
    }
}
