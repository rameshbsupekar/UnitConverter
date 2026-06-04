using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using UnitConverter.Common.Constants;

namespace UnitConverter.UserManagement.DataAccess.Data;

/// <summary>
/// Design-time factory for <c>dotnet ef</c> migrations (local SQLite under <c>Data/</c>).
/// </summary>
public sealed class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        DatabasePaths.EnsureSqliteDirectoriesExist();

        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlite(DatabasePaths.UserManagementConnectionString)
            .Options;

        return new AuthDbContext(options);
    }
}
