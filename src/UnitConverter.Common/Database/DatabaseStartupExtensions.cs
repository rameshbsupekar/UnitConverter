using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UnitConverter.Common.Constants;

namespace UnitConverter.Common.Database;

/// <summary>
/// Ensures the database exists and all EF migrations are applied before the host serves traffic.
/// </summary>
public static class DatabaseStartupExtensions
{
    public static async Task EnsureEfMigrationsAppliedAsync<TContext>(
        this IHost host,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();

        if (!await db.Database.CanConnectAsync(cancellationToken))
        {
            var connectionString = db.Database.GetConnectionString();
            throw new InvalidOperationException(
                BuildCannotConnectMessage(typeof(TContext).Name, connectionString));
        }

        var pending = await db.Database.GetPendingMigrationsAsync(cancellationToken);
        var pendingList = pending.ToList();
        if (pendingList.Count > 0)
        {
            throw new InvalidOperationException(
                $"Pending EF Core migrations for {typeof(TContext).Name}: {string.Join(", ", pendingList)}. " +
                "Run tools/UnitConverter.DbSetup (dotnet run --project tools/UnitConverter.DbSetup -- --seed) from the repository root to create local databases.");
        }
    }

    private static string BuildCannotConnectMessage(string contextName, string? connectionString)
    {
        var message =
            $"Cannot connect to the database for {contextName}. " +
            "Run tools/UnitConverter.DbSetup (dotnet run --project tools/UnitConverter.DbSetup -- --seed) from the repository root to create local databases.";

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return message;
        }

        message += Environment.NewLine + $"Connection: {RedactSecrets(connectionString)}";

        if (TryGetSqliteDataSource(connectionString, out var dataSource))
        {
            message += Environment.NewLine + $"SQLite file: {dataSource}";
            message += Environment.NewLine + $"File exists: {File.Exists(dataSource)}";
            message += Environment.NewLine +
                       $"Resolved Data root: {DatabasePaths.RepositoryDataRoot}";
        }

        return message;
    }

    private static string RedactSecrets(string connectionString)
    {
        const string passwordKey = "Password=";
        var idx = connectionString.IndexOf(passwordKey, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return connectionString;
        }

        var end = connectionString.IndexOf(';', idx);
        return end < 0
            ? connectionString[..idx] + "Password=***"
            : connectionString[..idx] + "Password=***;" + connectionString[(end + 1)..];
    }

    private static bool TryGetSqliteDataSource(string connectionString, out string dataSource)
    {
        dataSource = string.Empty;
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var segment = part.Trim();
            if (segment.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
            {
                dataSource = segment["Data Source=".Length..].Trim();
                return dataSource.Length > 0;
            }
        }

        return false;
    }
}
