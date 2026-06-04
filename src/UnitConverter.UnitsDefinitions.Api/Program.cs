using Scalar.AspNetCore;
using UnitConverter.UnitsDefinitions.Api.DependencyInjection;
using UnitConverter.Common.Constants;
using UnitConverter.Common.Database;
using UnitConverter.Common.DependencyInjection;
using UnitConverter.UnitsDefinitions.Api.Middleware;
using UnitConverter.UnitsDefinitions.DataAccess.DependencyInjection;
using UnitConverter.UnitsDefinitions.DataAccess.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApiSecurity();
builder.Services.AddOpenApi();
builder.Services.AddUrlSegmentApiVersioning();
builder.Services.AddSharedResilience(builder.Configuration);
builder.Services.AddUnitConverterCors(builder.Configuration, builder.Environment);
builder.Services.AddUnitConverterApiServices();
builder.Services.AddUnitConverterJwtAuthentication(builder.Configuration);

DatabasePaths.EnsureSqliteDirectoriesExist();
var unitsDb = DatabasePaths.ResolveUnitsMasterDataConnectionString(
    builder.Configuration.GetConnectionString(ConfigurationKeys.ConnectionStrings.UnitsMasterData));

builder.Services.AddUnitsMasterDataAccess(unitsDb);

var app = builder.Build();

if (!app.Environment.IsEnvironment(HostEnvironmentNames.Testing))
{
    await app.EnsureEfMigrationsAppliedAsync<ConverterDbContext>();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseUnitConverterAspNetExceptionHandling(app.Environment);

app.UseHttpsRedirection();
app.UseRouting();
app.UseUnitConverterCors();
app.UseRateLimiter();
app.UseUnitConverterApiSecurity();
app.UseUnitsDefinitionsDomainExceptionHandling();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();

/// <summary>Exposed for integration tests.</summary>
public partial class Program
{
    private Program() { }
}
