using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using UnitConverter.Common.Constants;
using UnitConverter.UnitsDefinitions.Contracts.Responses;
using UnitConverter.UnitsDefinitions.Contracts.Units;

namespace UnitConverter.UnitsDefinitions.Api.Tests;

[TestClass]
public sealed class CatalogApiTests : ApiIntegrationTestBase
{
    [TestMethod]
    public async Task GetCategories_WithoutToken_ReturnsAllCategories()
    {
        ClearAuthentication();

        var response = await Client.GetAsync(ApiV1Paths.CatalogCategories);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var categories = await response.Content.ReadFromJsonAsync<List<UnitCategoryResponse>>();
        Assert.IsNotNull(categories);
        Assert.AreEqual(3, categories.Count);
        Assert.IsTrue(categories.Exists(c => c.Id == (int)UnitCategory.Length && c.Name == "length"));
        Assert.IsTrue(categories.Exists(c => c.Id == (int)UnitCategory.Mass && c.Name == "mass"));
        Assert.IsTrue(categories.Exists(c => c.Id == (int)UnitCategory.Temperature && c.Name == "temperature"));
    }

    [TestMethod]
    public async Task GetCatalogUnits_WithoutToken_ReturnsApprovedUnits()
    {
        ClearAuthentication();

        var response = await Client.GetAsync($"{ApiV1Paths.CatalogUnits}?category=1&page=1&pageSize=50");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.IsTrue(json.TryGetProperty("items", out var items));
        Assert.IsGreaterThanOrEqualTo(5, items.GetArrayLength());
        Assert.IsTrue(items[0].TryGetProperty("symbol", out _));
        Assert.IsTrue(items[0].TryGetProperty("displayName", out _));
    }

    [TestMethod]
    public async Task GetCatalogUnits_ByCategoryName_ReturnsApprovedUnits()
    {
        ClearAuthentication();

        var response = await Client.GetAsync($"{ApiV1Paths.CatalogUnits}?categoryName=length&page=1&pageSize=50");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.IsTrue(json.TryGetProperty("items", out var items));
        Assert.IsGreaterThanOrEqualTo(5, items.GetArrayLength());
    }

    [TestMethod]
    public async Task GetCatalogUnits_ByWeightAlias_ReturnsMassUnits()
    {
        ClearAuthentication();

        var response = await Client.GetAsync($"{ApiV1Paths.CatalogUnits}?categoryName=weight&page=1&pageSize=50");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.IsTrue(json.TryGetProperty("items", out var items));
        Assert.IsTrue(
            items.EnumerateArray().Any(i =>
                i.TryGetProperty("symbol", out var s) && s.GetString() == "kg"));
    }

    [TestMethod]
    public async Task GetCatalogUnits_UnknownCategoryName_Returns400()
    {
        ClearAuthentication();

        var response = await Client.GetAsync($"{ApiV1Paths.CatalogUnits}?categoryName=volume");

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task PostConversions_WithoutToken_Returns200()
    {
        ClearAuthentication();

        var request = new { value = 1000m, fromUnit = "m", toUnit = "km", category = 1 };
        var response = await Client.PostAsJsonAsync(ApiV1Paths.UnitConversions, request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
}
