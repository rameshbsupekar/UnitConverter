using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UnitConverter.Auth.Common.Interfaces;
using UnitConverter.Auth.Infrastructure.Data;
using UnitConverter.Auth.Infrastructure.Repositories;

namespace UnitConverter.Auth.Infrastructure.Extensions;

/// <summary>
/// Dependency injection extensions for registering infrastructure services.
/// Includes DbContext, repositories, and Unit of Work pattern.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all infrastructure services (DbContext, repositories, UnitOfWork).
    /// Supports both SQL Server and SQLite database providers.
    /// </summary>
    /// <param name="services">The IServiceCollection to register services to.</param>
    /// <param name="connectionString">Database connection string.</param>
    /// <param name="useSqlite">Whether to use SQLite; if false, uses SQL Server.</param>
    /// <returns>The IServiceCollection for method chaining.</returns>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        string connectionString,
        bool useSqlite = false)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be empty.", nameof(connectionString));

        services.AddDbContext<AuthDbContext>(options =>
        {
            if (useSqlite)
            {
                options.UseSqlite(connectionString);
            }
            else
            {
                options.UseSqlServer(connectionString);
            }
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    /// <summary>
    /// Adds database health checks to the service collection.
    /// Health check verifies EF Core can connect and query the database.
    /// </summary>
    /// <param name="services">The IServiceCollection to register services to.</param>
    /// <returns>The IHealthChecksBuilder for further configuration.</returns>
    /// <remarks>
    /// Note: Disabled due to package version constraints. HealthChecks infrastructure
    /// can be added when appropriate package versions are available.
    /// </remarks>
    public static IServiceCollection AddDatabaseHealthChecks(this IServiceCollection services)
    {
        // Placeholder implementation - health checks infrastructure disabled for now
        // To enable: uncomment code and add Microsoft.Extensions.Diagnostics.HealthChecks package
        /*
        services.AddHealthChecks()
            .AddDbContextCheck<AuthDbContext>(
                name: "AuthDbContext",
                tags: new[] { "database", "auth" });
        */

        return services;
    }
}
