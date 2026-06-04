using Scalar.AspNetCore;
using UnitConverter.UserManagement.Api.Middleware;
using UnitConverter.UserManagement.Application.DependencyInjection;
using UnitConverter.UserManagement.DataAccess.Data;
using UnitConverter.UserManagement.DataAccess.Extensions;
using UnitConverter.Common.Constants;
using UnitConverter.Common.Database;
using UnitConverter.Common.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApiSecurity();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddUrlSegmentApiVersioning();
builder.Services.AddAuthApplicationServices(builder.Configuration);

var connectionString = DatabasePaths.ResolveUserManagementConnectionString(
    builder.Configuration.GetConnectionString(ConfigurationKeys.ConnectionStrings.UserManagement));
DatabasePaths.EnsureSqliteDirectoriesExist();
builder.Services.AddUserManagementDataAccess(connectionString, useSqlite: true);
builder.Services.AddSharedResilience(builder.Configuration);
builder.Services.AddUnitConverterCors(builder.Configuration, builder.Environment);

var app = builder.Build();

if (!app.Environment.IsEnvironment(HostEnvironmentNames.Testing))
{
    await app.EnsureEfMigrationsAppliedAsync<AuthDbContext>();
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
app.UseAuthMiddleware();
app.UseAuthorization();
app.MapHealthChecks(EndpointPaths.Health);
app.MapControllers();

await app.RunAsync();

public partial class Program
{
    private Program() { }
}
