namespace UnitConverter.Common.Constants;

/// <summary>
/// SQLite file locations under the repository <c>Data/</c> folder (outside <c>src/</c>).
/// Cloud deployments override <c>ConnectionStrings</c> in configuration.
/// </summary>
public static class DatabasePaths
{
    public const string DataRootFolderName = "Data";

    /// <summary>Folder under <see cref="DataRootFolderName"/> for the Auth / user-management database.</summary>
    public const string UserManagementFolder = "UserManagement";

    /// <summary>Folder under <see cref="DataRootFolderName"/> for the units master-data database.</summary>
    public const string UnitsMasterFolder = "UnitsMaster";

    public const string UserManagementDatabaseFile = "auth.db";
    public const string UnitsMasterDatabaseFile = "catalog.db";

    public static string RepositoryDataRoot => LocateRepositoryDataRoot();

    public static string UserManagementDirectory =>
        Path.Combine(RepositoryDataRoot, UserManagementFolder);

    public static string UnitsMasterDirectory =>
        Path.Combine(RepositoryDataRoot, UnitsMasterFolder);

    public static string UserManagementDatabasePath =>
        Path.Combine(UserManagementDirectory, UserManagementDatabaseFile);

    public static string UnitsMasterDatabasePath =>
        Path.Combine(UnitsMasterDirectory, UnitsMasterDatabaseFile);

    /// <summary>SQLite connection for <c>ConnectionStrings:UserManagement</c> when unset in appsettings.</summary>
    public static string UserManagementConnectionString =>
        $"Data Source={UserManagementDatabasePath};Cache=Shared";

    /// <summary>SQLite connection for <c>ConnectionStrings:UnitsMasterData</c> when unset in appsettings.</summary>
    public static string UnitsMasterDataConnectionString =>
        $"Data Source={UnitsMasterDatabasePath}";

    public static void EnsureSqliteDirectoriesExist()
    {
        Directory.CreateDirectory(UserManagementDirectory);
        Directory.CreateDirectory(UnitsMasterDirectory);
    }

    /// <summary>
    /// Uses <paramref name="configured"/> when set; otherwise the default SQLite file under <see cref="DataRootFolderName"/>.
    /// </summary>
    public static string ResolveUserManagementConnectionString(string? configured) =>
        string.IsNullOrWhiteSpace(configured) ? UserManagementConnectionString : configured;

    /// <summary>
    /// Uses <paramref name="configured"/> when set; otherwise the default SQLite file under <see cref="DataRootFolderName"/>.
    /// </summary>
    public static string ResolveUnitsMasterDataConnectionString(string? configured) =>
        string.IsNullOrWhiteSpace(configured) ? UnitsMasterDataConnectionString : configured;

    private static string LocateRepositoryDataRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var dataRoot = Path.Combine(directory.FullName, DataRootFolderName);
            if (Directory.Exists(dataRoot))
            {
                return dataRoot;
            }

            if (File.Exists(Path.Combine(directory.FullName, "UnitConverter.slnx")))
            {
                return Path.Combine(directory.FullName, DataRootFolderName);
            }

            directory = directory.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, DataRootFolderName);
    }
}
