using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UnitConverter.UnitsDefinitions.Catalog;
using UnitConverter.UnitsDefinitions.DataAccess.Persistence;

namespace UnitConverter.UnitsDefinitions.Api.Tests;

/// <summary>
/// Configured host for conversion API integration tests (instance-based, not static).
/// </summary>
public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"unitconverter-api-test-{Guid.NewGuid():N}.db");
        builder.UseSetting(WebHostDefaults.EnvironmentKey, "Testing");
        builder.UseSetting("ConnectionStrings:UnitsMasterData", $"Data Source={dbPath}");
        builder.UseSetting("ASPNETCORE_HTTPS_PORT", "7100");
        base.ConfigureWebHost(builder);
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ConverterDbContext>();
        db.Database.Migrate();
        UnitCatalogSeedData.SeedAsync(db).GetAwaiter().GetResult();

        return host;
    }
}
