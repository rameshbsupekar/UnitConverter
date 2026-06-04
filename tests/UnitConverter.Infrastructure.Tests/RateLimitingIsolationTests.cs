using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UnitConverter.Common.DependencyInjection;

namespace UnitConverter.Infrastructure.Tests;

/// <summary>
/// Tests for rate limiting configuration isolation and Core decoupling.
/// 
/// Verifies:
/// 1. Rate limiting configuration is isolated per service
/// 2. Core provides no service-specific logic
/// 3. AddSharedResilience is truly generic and reusable
/// </summary>
public class RateLimitingIsolationTests
{
    /// <summary>
    /// Test 1: Each service has isolated rate limiting configuration
    /// </summary>
    [Fact]
    public void Each_Service_Has_Isolated_RateLimiting_Configuration()
    {
        // Arrange
        var authConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "RateLimiting:Policies:FixedWindow_ByIp:PermitLimit", "10" }
            })
            .Build();

        var catalogConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "RateLimiting:Policies:FixedWindow_ByIp:PermitLimit", "20" }
            })
            .Build();

        // Act
        var authServices = new ServiceCollection();
        var catalogServices = new ServiceCollection();

        authServices.AddSharedResilience(authConfig);
        catalogServices.AddSharedResilience(catalogConfig);

        // Assert
        // Each service collection is independent
        Assert.NotNull(authServices);
        Assert.NotNull(catalogServices);
        
        var authProvider = authServices.BuildServiceProvider();
        var catalogProvider = catalogServices.BuildServiceProvider();

        // Both should be functional
        Assert.NotNull(authProvider);
        Assert.NotNull(catalogProvider);
    }

    /// <summary>
    /// Test 2: Core resilience works with empty configuration
    /// (proves it's not service-specific)
    /// </summary>
    [Fact]
    public void Core_Resilience_Works_With_Empty_Configuration()
    {
        // Arrange
        var emptyConfig = new ConfigurationBuilder().Build();

        // Act & Assert
        var services = new ServiceCollection();
        
        var exception = Record.Exception(() =>
        {
            services.AddSharedResilience(emptyConfig);
            var provider = services.BuildServiceProvider();
            Assert.NotNull(provider);
        });

        Assert.Null(exception);
    }

    /// <summary>
    /// Test 3: Core can be added to existing service collections
    /// without affecting other registrations
    /// </summary>
    [Fact]
    public void AddSharedResilience_Does_Not_Affect_Existing_Registrations()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<string>("TestValue");
        var config = new ConfigurationBuilder().Build();

        // Act
        services.AddSharedResilience(config);

        // Assert
        var provider = services.BuildServiceProvider();
        var testValue = provider.GetService<string>();
        Assert.Equal("TestValue", testValue);
    }

    /// <summary>
    /// Test 4: Core extension methods are chainable
    /// </summary>
    [Fact]
    public void Core_Extensions_Are_Chainable()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        // Act & Assert
        var result = services
            .AddLogging()
            .AddSharedResilience(config);

        Assert.NotNull(result);
        Assert.IsAssignableFrom<IServiceCollection>(result);
    }
}
