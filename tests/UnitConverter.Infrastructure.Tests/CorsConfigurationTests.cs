using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using UnitConverter.Common.DependencyInjection;
using Xunit;

namespace UnitConverter.Infrastructure.Tests;

public sealed class CorsConfigurationTests
{
    [Theory]
    [InlineData("http://localhost:3000")]
    [InlineData("https://localhost:7100")]
    [InlineData("http://127.0.0.1:5173")]
    [InlineData("https://[::1]:8080")]
    public void IsLocalhostOrigin_LocalhostAndLoopback_ReturnsTrue(string origin)
    {
        Assert.True(CorsServiceCollectionExtensions.IsLocalhostOrigin(origin));
    }

    [Theory]
    [InlineData("https://evil.localhost.attacker.com")]
    [InlineData("https://example.com")]
    public void IsLocalhostOrigin_NonLocalhost_ReturnsFalse(string origin)
    {
        Assert.False(CorsServiceCollectionExtensions.IsLocalhostOrigin(origin));
    }

    [Fact]
    public void ResolveCorsOptions_WhenDevelopmentAndEmpty_AllowsAnyLocalhostPort()
    {
        var configuration = new ConfigurationBuilder().Build();
        var environment = new TestHostEnvironment(Environments.Development);

        var options = CorsServiceCollectionExtensions.ResolveCorsOptions(configuration, environment);

        Assert.True(options.AllowAnyLocalhostPort);
        Assert.True(options.IsEnabled);
    }

    [Fact]
    public void ResolveCorsOptions_WhenProductionAndEmpty_IsDisabled()
    {
        var configuration = new ConfigurationBuilder().Build();
        var environment = new TestHostEnvironment(Environments.Production);

        var options = CorsServiceCollectionExtensions.ResolveCorsOptions(configuration, environment);

        Assert.False(options.AllowAnyLocalhostPort);
        Assert.False(options.IsEnabled);
    }

    [Fact]
    public void ResolveCorsOptions_WhenExplicitOriginInProduction_IsEnabled()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Cors:AllowedOrigins:0", "https://app.example.com" }
            })
            .Build();

        var environment = new TestHostEnvironment(Environments.Production);

        var options = CorsServiceCollectionExtensions.ResolveCorsOptions(configuration, environment);

        Assert.Single(options.ExplicitOrigins);
        Assert.True(options.IsEnabled);
    }

    [Fact]
    public void ResolveCorsOptions_WhenTesting_IsDisabled()
    {
        var configuration = new ConfigurationBuilder().Build();
        var environment = new TestHostEnvironment(Environments.Development, isTesting: true);

        var options = CorsServiceCollectionExtensions.ResolveCorsOptions(configuration, environment);

        Assert.False(options.IsEnabled);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string environmentName, bool isTesting = false)
        {
            EnvironmentName = isTesting ? "Testing" : environmentName;
            ApplicationName = "UnitConverter.Tests";
            ContentRootPath = AppContext.BaseDirectory;
            ContentRootFileProvider = new NullFileProvider();
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; }
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }
    }
}
