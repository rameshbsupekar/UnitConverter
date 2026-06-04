using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using UnitConverter.Common.Constants;
using UnitConverter.UnitsDefinitions.Contracts.Requests;
using UnitConverter.UnitsDefinitions.Contracts.Responses;
using UnitConverter.UnitsDefinitions.Contracts.Units;

namespace UnitConverter.UnitsDefinitions.Api.Tests;

[TestClass]
public sealed class ConversionsApiTests : ApiIntegrationTestBase
{
    [TestMethod]
    public async Task PostConversions_MetersToKilometers_Returns200AndConvertedValue()
    {
        var request = new ConvertUnitsRequest(1000m, "m", "km", UnitCategory.Length);

        var response = await Client.PostAsJsonAsync(ApiV1Paths.UnitConversions, request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ConversionResponse>();
        Assert.IsNotNull(body);
        Assert.AreEqual(1m, body.Value);
        Assert.AreEqual("km", body.Symbol);
    }

    [TestMethod]
    public async Task PostConversions_CelsiusToFahrenheit_Returns32()
    {
        var request = new ConvertUnitsRequest(0m, "c", "f", UnitCategory.Temperature);

        var response = await Client.PostAsJsonAsync(ApiV1Paths.UnitConversions, request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ConversionResponse>();
        Assert.IsNotNull(body);
        Assert.AreEqual(32m, body.Value, 0.0001m);
        Assert.AreEqual("f", body.Symbol);
    }

    [TestMethod]
    public async Task PostConversions_UnknownUnit_Returns400()
    {
        var request = new ConvertUnitsRequest(1m, "not-a-unit", "m", UnitCategory.Length);

        var response = await Client.PostAsJsonAsync(ApiV1Paths.UnitConversions, request);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.IsTrue(json.TryGetProperty("detail", out var detail));
        Assert.IsFalse(string.IsNullOrWhiteSpace(detail.GetString()));
    }

    [TestMethod]
    public async Task GetCatalogUnits_LengthCategory_ReturnsSeededUnits()
    {
        var response = await Client.GetAsync($"{ApiV1Paths.CatalogUnits}?category=1&page=1&pageSize=50");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.IsTrue(json.TryGetProperty("items", out var items));
        Assert.IsGreaterThanOrEqualTo(5, items.GetArrayLength());
        Assert.IsTrue(items[0].TryGetProperty("symbol", out var symbol));
        Assert.IsFalse(string.IsNullOrWhiteSpace(symbol.GetString()));
    }
}
