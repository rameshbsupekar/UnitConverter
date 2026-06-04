using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace UnitConverter.UserManagement.Api.Tests;

/// <summary>
/// Test host for User Management API (auth only).
/// </summary>
public sealed class AuthWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"unitconverter-auth-test-{Guid.NewGuid():N}.db");
        builder.UseSetting(WebHostDefaults.EnvironmentKey, "Testing");
        builder.UseSetting("ConnectionStrings:UserManagement", $"Data Source={dbPath}");
        base.ConfigureWebHost(builder);
    }
}
