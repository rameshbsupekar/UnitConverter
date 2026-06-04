using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UnitConverter.UserManagement.Common.Interfaces;
using UnitConverter.UserManagement.DataAccess.Data;
using UnitConverter.UserManagement.DataAccess.Repositories;

namespace UnitConverter.UserManagement.DataAccess.Extensions;

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
    public static IServiceCollection AddUserManagementDataAccess(
        this IServiceCollection services,
        string connectionString,
        bool useSqlite = false)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be empty.", nameof(connectionString));

        services.AddDbContext<AuthDbContext>(
            options =>
            {
                if (useSqlite)
                {
                    options.UseSqlite(connectionString);
                }
                else
                {
                    options.UseSqlServer(connectionString);
                }
            },
            contextLifetime: ServiceLifetime.Scoped,
            optionsLifetime: ServiceLifetime.Scoped);

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
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
    public static IServiceCollection AddDatabaseHealthChecks(this IServiceCollection services) =>
        services;
}
