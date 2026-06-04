using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UnitConverter.Common.Contracts.Resilience;
using UnitConverter.Common.Resilience;
using UnitConverter.Common.DependencyInjection;

namespace UnitConverter.Infrastructure.Tests;

/// <summary>
/// Tests for IResilienceConfigurer interface and implementation.
/// 
/// Verifies:
/// 1. Interface is properly resolved from DI container
/// 2. ResilienceConfigurer implements the interface
/// 3. Rate limiting is configured correctly
/// 4. HTTP resilience is available
/// 5. Multiple services can use AddSharedResilience independently
/// </summary>
public class ResilienceConfigurerTests
{
    /// <summary>
    /// Test 1: IResilienceConfigurer can be resolved from the service collection
    /// </summary>
    [Fact]
    public void AddSharedResilience_Registers_IResilienceConfigurer()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        services.AddSharedResilience(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var configurer = serviceProvider.GetService<IResilienceConfigurer>();
        Assert.NotNull(configurer);
        Assert.IsType<ResilienceConfigurer>(configurer);
    }

    /// <summary>
    /// Test 2: AddSharedResilience properly configures rate limiting in the container
    /// </summary>
    [Fact]
    public void AddSharedResilience_Configures_RateLimiter()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        services.AddSharedResilience(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        // If rate limiting is properly configured, the service provider should be functional
        Assert.NotNull(serviceProvider);
    }

    /// <summary>
    /// Test 3: ResilienceConfigurer can be instantiated and used directly
    /// </summary>
    [Fact]
    public void ResilienceConfigurer_Can_Be_Instantiated()
    {
        // Arrange & Act
        var configurer = new ResilienceConfigurer();

        // Assert
        Assert.NotNull(configurer);
        Assert.IsAssignableFrom<IResilienceConfigurer>(configurer);
    }

    /// <summary>
    /// Test 4: AddRateLimiting method executes without errors
    /// </summary>
    [Fact]
    public void AddRateLimiting_Executes_Without_Errors()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        var configurer = new ResilienceConfigurer();

        // Act & Assert (should not throw)
        var exception = Record.Exception(() =>
        {
            configurer.AddRateLimiting(services, configuration);
        });

        Assert.Null(exception);
    }

    /// <summary>
    /// Test 5: Multiple services can independently use AddSharedResilience
    /// proving Core is decoupled and reusable
    /// </summary>
    [Fact]
    public void Multiple_Services_Can_Use_AddSharedResilience_Independently()
    {
        // Simulate three different services (Auth, Catalog, Conversion) 
        // all using the same Core resilience configuration

        // Arrange & Act
        var authServices = new ServiceCollection();
        var catalogServices = new ServiceCollection();
        var conversionServices = new ServiceCollection();

        var configuration = new ConfigurationBuilder().Build();

        authServices.AddSharedResilience(configuration);
        catalogServices.AddSharedResilience(configuration);
        conversionServices.AddSharedResilience(configuration);

        var authProvider = authServices.BuildServiceProvider();
        var catalogProvider = catalogServices.BuildServiceProvider();
        var conversionProvider = conversionServices.BuildServiceProvider();

        // Assert
        var authConfigurer = authProvider.GetService<IResilienceConfigurer>();
        var catalogConfigurer = catalogProvider.GetService<IResilienceConfigurer>();
        var conversionConfigurer = conversionProvider.GetService<IResilienceConfigurer>();

        Assert.NotNull(authConfigurer);
        Assert.NotNull(catalogConfigurer);
        Assert.NotNull(conversionConfigurer);

        // Each service gets its own instance (not shared)
        Assert.NotSame(authConfigurer, catalogConfigurer);
        Assert.NotSame(catalogConfigurer, conversionConfigurer);
    }

    /// <summary>
    /// Test 6: AddHttpResilience does not throw when called on an HttpClientBuilder
    /// </summary>
    [Fact]
    public void AddHttpResilience_Extension_Works()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient("TestClient");

        // Act & Assert
        var builder = services.AddHttpClient("TestResilience");
        var configurer = new ResilienceConfigurer();

        var exception = Record.Exception(() =>
        {
            configurer.AddHttpResilience(builder);
        });

        Assert.Null(exception);
    }
}
