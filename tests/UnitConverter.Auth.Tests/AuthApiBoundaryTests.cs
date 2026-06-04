using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using UnitConverter.Common.Constants;
using UnitConverter.UserManagement.Api.Controllers;

namespace UnitConverter.UserManagement.Api.Tests;

/// <summary>
/// User Management hosts identity and sessions only — no catalog, conversion, or unit-definition routes.
/// </summary>
[TestClass]
public sealed class AuthApiBoundaryTests
{
    private static AuthWebApplicationFactory? _factory;
    private HttpClient? _client;

    [TestInitialize]
    public void Initialize() =>
        _client = (_factory ??= new AuthWebApplicationFactory()).CreateClient();

    [TestCleanup]
    public void Cleanup()
    {
        _client?.Dispose();
        _client = null;
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _factory?.Dispose();
        _factory = null;
    }

    [TestMethod]
    public void Controllers_OnlyAuthControllerIsRegistered()
    {
        var controllers = typeof(Program).Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t))
            .ToList();

        Assert.AreEqual(1, controllers.Count);
        Assert.AreEqual(typeof(AuthController), controllers[0]);
    }

    [TestMethod]
    public void AuthController_DoesNotExposeUnitsCatalogOrConversionRoutes()
    {
        var methods = typeof(AuthController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttribute<HttpMethodAttribute>() is not null);

        foreach (var method in methods)
        {
            var template = method.GetCustomAttribute<HttpMethodAttribute>()!.Template ?? string.Empty;
            Assert.IsFalse(
                template.Contains("catalog", StringComparison.OrdinalIgnoreCase),
                $"Unexpected catalog route on {method.Name}");
            Assert.IsFalse(
                template.Contains("unit-definitions", StringComparison.OrdinalIgnoreCase),
                $"Unexpected unit-definitions route on {method.Name}");
            Assert.IsFalse(
                template.Contains("unit-conversions", StringComparison.OrdinalIgnoreCase),
                $"Unexpected unit-conversions route on {method.Name}");
        }
    }

    [DataTestMethod]
    [DataRow(ApiV1Paths.CatalogCategories)]
    [DataRow($"{ApiV1Paths.CatalogUnits}?categoryName=length")]
    [DataRow(ApiV1Paths.UnitConversions)]
    [DataRow(ApiV1Paths.UnitDefinitions)]
    public async Task UnitsRoutes_ReturnNotFoundOnAuthHost(string path)
    {
        var response = await _client!.GetAsync(path);
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task CreateSession_ExistsOnAuthHost()
    {
        var response = await _client!.PostAsJsonAsync(
            ApiV1Paths.CreateSession,
            new { email = "missing@unitconverter.local", password = "Test@12345" });

        Assert.AreNotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
