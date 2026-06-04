using System.Net;
using System.Net.Http.Json;
using UnitConverter.Common.Constants;
using UnitConverter.Common.Security;
using UnitConverter.UnitsDefinitions.Contracts.Requests;
using UnitConverter.UnitsDefinitions.Contracts.Responses;
using UnitConverter.UnitsDefinitions.Contracts.Units;

namespace UnitConverter.UnitsDefinitions.Api.Tests;

[TestClass]
public sealed class UnitDefinitionsApiTests : ApiIntegrationTestBase
{
    [TestMethod]
    public async Task MasterData_WithoutToken_Returns401()
    {
        ClearAuthentication();
        var response = await Client.GetAsync(ApiV1Paths.UnitDefinitions);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task MasterData_AsPublic_Returns403()
    {
        AuthenticateAs(RoleNames.Public);
        var response = await Client.GetAsync(ApiV1Paths.UnitDefinitions);
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Employee_CreateAndUpdateUnit_Succeeds()
    {
        AuthenticateAs(RoleNames.Employee);
        var create = new CreateUnitRequest("rd", "Rod", UnitCategory.Length, 5.0292m);
        var createResponse = await Client.PostAsJsonAsync(ApiV1Paths.UnitDefinitions, create);
        Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<UnitDetailResponse>();
        Assert.IsNotNull(created);
        Assert.AreEqual("rd", created.Symbol);

        var update = new UpdateUnitRequest("Rod (survey)", 5.0292m);
        var updateResponse = await Client.PutAsJsonAsync(
            ApiV1Paths.UnitDefinitionById(created.Id),
            update);
        Assert.AreEqual(HttpStatusCode.OK, updateResponse.StatusCode);

        var getResponse = await Client.GetAsync(ApiV1Paths.UnitDefinitionById(created.Id));
        Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);
        var loaded = await getResponse.Content.ReadFromJsonAsync<UnitDetailResponse>();
        Assert.AreEqual("Rod (survey)", loaded!.DisplayName);
    }

    [TestMethod]
    public async Task Employee_DeleteUnit_Returns403()
    {
        AuthenticateAs(RoleNames.Employee);
        var createResponse = await Client.PostAsJsonAsync(
            ApiV1Paths.UnitDefinitions,
            new CreateUnitRequest("ch", "Chain", UnitCategory.Length, 20.1168m));
        var created = await createResponse.Content.ReadFromJsonAsync<UnitDetailResponse>();
        Assert.IsNotNull(created);

        var deleteResponse = await Client.DeleteAsync(ApiV1Paths.UnitDefinitionById(created!.Id));
        Assert.AreEqual(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }

    [TestMethod]
    public async Task Admin_DeleteUnit_Returns204()
    {
        AuthenticateAs(RoleNames.Admin);
        var createResponse = await Client.PostAsJsonAsync(
            ApiV1Paths.UnitDefinitions,
            new CreateUnitRequest("fur", "Furlong", UnitCategory.Length, 201.168m));
        var created = await createResponse.Content.ReadFromJsonAsync<UnitDetailResponse>();
        Assert.IsNotNull(created);

        var deleteResponse = await Client.DeleteAsync(ApiV1Paths.UnitDefinitionById(created!.Id));
        Assert.AreEqual(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var missingResponse = await Client.GetAsync(ApiV1Paths.UnitDefinitionById(created.Id));
        Assert.AreEqual(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [TestMethod]
    public async Task Partner_CreateDuplicateSymbol_Returns409()
    {
        AuthenticateAs(RoleNames.Partner);
        var request = new CreateUnitRequest("m", "Duplicate meter", UnitCategory.Length, 1m);
        var response = await Client.PostAsJsonAsync(ApiV1Paths.UnitDefinitions, request);
        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
    }

    [TestMethod]
    public async Task Employee_CreateUnit_WithInvalidPayload_Returns400ProblemDetails()
    {
        AuthenticateAs(RoleNames.Employee);
        var response = await Client.PostAsJsonAsync(
            ApiV1Paths.UnitDefinitions,
            new CreateUnitRequest(string.Empty, string.Empty, UnitCategory.Length, 0m));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.IsTrue(
            response.Content.Headers.ContentType?.MediaType?.Contains("problem+json", StringComparison.OrdinalIgnoreCase));
        var body = await response.Content.ReadAsStringAsync();
        StringAssert.Contains(body, "Symbol");
        StringAssert.Contains(body, "MultiplierToBase");
    }
}
