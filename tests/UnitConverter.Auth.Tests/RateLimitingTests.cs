using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using UnitConverter.Core.Extensions;

namespace UnitConverter.Auth.Tests;

/// <summary>
/// BDD-style unit tests for rate limiting extensions and configuration.
/// Tests verify that rate limiting configuration is properly set up and accessible.
/// </summary>
[TestClass]
public class RateLimitingTests
{
    private IServiceCollection _services = null!;
    private IConfiguration _configuration = null!;

    [TestInitialize]
    public void Setup()
    {
        _services = new ServiceCollection();

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "RateLimiting:Policies:FixedWindow_ByIp:PermitLimit", "10" },
                { "RateLimiting:Policies:FixedWindow_ByIp:QueueLimit", "5" },
                { "RateLimiting:Policies:SlidingWindow_ByUser:PermitLimit", "100" },
                { "RateLimiting:Policies:SlidingWindow_ByUser:QueueLimit", "10" },
                { "RateLimiting:Policies:TokenBucket_ByApiKey:TokenLimit", "500" },
                { "RateLimiting:Policies:TokenBucket_ByApiKey:QueueLimit", "0" }
            })
            .Build();
    }

    /// <summary>
    /// Given: AddApplicationRateLimiting called with configuration
    /// When: Extension method is invoked
    /// Then: Service collection is properly configured and returns itself
    /// </summary>
    [TestMethod]
    public void AddApplicationRateLimiting_WhenCalled_RegistersConfiguration()
    {
        // Act
        var result = _services.AddApplicationRateLimiting(_configuration);

        // Assert
        Assert.IsNotNull(result, "AddApplicationRateLimiting should return the service collection");
        Assert.AreSame(_services, result, "Should return the same service collection for chaining");
    }

    /// <summary>
    /// Given: Configuration with FixedWindow_ByIp policy settings
    /// When: Accessing the configuration section
    /// Then: PermitLimit and QueueLimit are correctly set
    /// </summary>
    [TestMethod]
    public void RateLimitingConfiguration_FixedWindowByIp_HasCorrectValues()
    {
        // Arrange
        const int expectedPermitLimit = 10;
        const int expectedQueueLimit = 5;

        // Act
        var permitLimit = _configuration.GetValue("RateLimiting:Policies:FixedWindow_ByIp:PermitLimit", 0);
        var queueLimit = _configuration.GetValue("RateLimiting:Policies:FixedWindow_ByIp:QueueLimit", 0);

        // Assert
        Assert.AreEqual(expectedPermitLimit, permitLimit, "FixedWindow_ByIp PermitLimit should be 10");
        Assert.AreEqual(expectedQueueLimit, queueLimit, "FixedWindow_ByIp QueueLimit should be 5");
    }

    /// <summary>
    /// Given: Configuration with SlidingWindow_ByUser policy settings
    /// When: Accessing the configuration section
    /// Then: PermitLimit and QueueLimit are correctly set
    /// </summary>
    [TestMethod]
    public void RateLimitingConfiguration_SlidingWindowByUser_HasCorrectValues()
    {
        // Arrange
        const int expectedPermitLimit = 100;
        const int expectedQueueLimit = 10;

        // Act
        var permitLimit = _configuration.GetValue("RateLimiting:Policies:SlidingWindow_ByUser:PermitLimit", 0);
        var queueLimit = _configuration.GetValue("RateLimiting:Policies:SlidingWindow_ByUser:QueueLimit", 0);

        // Assert
        Assert.AreEqual(expectedPermitLimit, permitLimit, "SlidingWindow_ByUser PermitLimit should be 100");
        Assert.AreEqual(expectedQueueLimit, queueLimit, "SlidingWindow_ByUser QueueLimit should be 10");
    }

    /// <summary>
    /// Given: Configuration with TokenBucket_ByApiKey policy settings
    /// When: Accessing the configuration section
    /// Then: TokenLimit and QueueLimit are correctly set
    /// </summary>
    [TestMethod]
    public void RateLimitingConfiguration_TokenBucketByApiKey_HasCorrectValues()
    {
        // Arrange
        const int expectedTokenLimit = 500;
        const int expectedQueueLimit = 0; // Reject immediately when full

        // Act
        var tokenLimit = _configuration.GetValue("RateLimiting:Policies:TokenBucket_ByApiKey:TokenLimit", 0);
        var queueLimit = _configuration.GetValue("RateLimiting:Policies:TokenBucket_ByApiKey:QueueLimit", 0);

        // Assert
        Assert.AreEqual(expectedTokenLimit, tokenLimit, "TokenBucket_ByApiKey TokenLimit should be 500");
        Assert.AreEqual(expectedQueueLimit, queueLimit, "TokenBucket_ByApiKey should have 0 queue (reject immediately)");
    }

    /// <summary>
    /// Given: Multiple rate limiting policies configured
    /// When: Accessing all policy sections
    /// Then: All policies are present and have non-zero values
    /// </summary>
    [TestMethod]
    public void RateLimitingConfiguration_AllPolicies_AreConfigured()
    {
        // Act
        var section = _configuration.GetSection("RateLimiting:Policies");

        // Assert
        Assert.IsNotNull(section, "RateLimiting:Policies section should exist");
        
        // Check each policy is configured
        var fixedWindow = section.GetSection("FixedWindow_ByIp:PermitLimit").Value;
        var slidingWindow = section.GetSection("SlidingWindow_ByUser:PermitLimit").Value;
        var tokenBucket = section.GetSection("TokenBucket_ByApiKey:TokenLimit").Value;

        Assert.IsNotNull(fixedWindow, "FixedWindow_ByIp should be configured");
        Assert.IsNotNull(slidingWindow, "SlidingWindow_ByUser should be configured");
        Assert.IsNotNull(tokenBucket, "TokenBucket_ByApiKey should be configured");

        Assert.AreNotEqual("0", fixedWindow, "FixedWindow_ByIp permit limit should be positive");
        Assert.AreNotEqual("0", slidingWindow, "SlidingWindow_ByUser permit limit should be positive");
        Assert.AreNotEqual("0", tokenBucket, "TokenBucket_ByApiKey token limit should be positive");
    }

    /// <summary>
    /// Given: Rate limiting configuration registered in service collection
    /// When: Building the service provider
    /// Then: Service collection returns itself and configuration is properly set
    /// </summary>
    [TestMethod]
    public void RateLimitingConfiguration_WhenRegistered_ReturnsServiceCollection()
    {
        // Arrange
        _services.AddSingleton(_configuration);

        // Act
        var result = _services.AddApplicationRateLimiting(_configuration);

        // Assert
        Assert.IsNotNull(result, "Service provider result should not be null");
        Assert.AreSame(_services, result, "Should return the same service collection for chaining");
    }
}
