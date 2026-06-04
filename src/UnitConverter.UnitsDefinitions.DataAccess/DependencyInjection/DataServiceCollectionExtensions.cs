using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UnitConverter.UnitsDefinitions.Catalog;
using UnitConverter.UnitsDefinitions.DataAccess.Persistence;
using UnitConverter.UnitsDefinitions.DataAccess.Repositories;

namespace UnitConverter.UnitsDefinitions.DataAccess.DependencyInjection;

public static class UnitsMasterDataAccessServiceCollectionExtensions
{
    public static string DefaultSqliteConnectionString =>
        UnitConverter.Common.Constants.DatabasePaths.UnitsMasterDataConnectionString;

    public static IServiceCollection AddUnitsMasterDataAccess(
        this IServiceCollection services,
        string? connectionString = null)
    {
        var cs = connectionString ?? DefaultSqliteConnectionString;

        services.AddDbContext<ConverterDbContext>(
            options => options.UseSqlite(cs),
            contextLifetime: ServiceLifetime.Scoped,
            optionsLifetime: ServiceLifetime.Scoped);

        services.AddScoped<IUnitCatalogRepository, UnitCatalogRepository>();
        services.AddScoped<IUnitCatalogAdminRepository, UnitCatalogAdminRepository>();

        return services;
    }
}
