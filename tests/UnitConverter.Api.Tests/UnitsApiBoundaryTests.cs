using System.Net;
using System.Net.Http.Json;
using UnitConverter.Common.Constants;

namespace UnitConverter.UnitsDefinitions.Api.Tests;

/// <summary>
/// Units Definitions API does not host user registration or sessions — those live on User Management only.
/// </summary>
[TestClass]
public sealed class UnitsApiBoundaryTests : ApiIntegrationTestBase
{
    [DataTestMethod]
    [DataRow(ApiV1Paths.RegisterUser)]
    [DataRow(ApiV1Paths.CreateSession)]
    [DataRow(ApiV1Paths.RefreshSession)]
    public async Task IdentityRoutes_ReturnNotFoundOnUnitsHost(string path)
    {
        var response = await Client.PostAsJsonAsync(path, new { });
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
