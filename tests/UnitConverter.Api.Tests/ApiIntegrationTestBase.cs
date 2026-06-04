using System.Net.Http.Headers;
using UnitConverter.Common.Security;

namespace UnitConverter.UnitsDefinitions.Api.Tests;

/// <summary>
/// Shared instance factory per test class (avoids static test host).
/// </summary>
public abstract class ApiIntegrationTestBase
{
    private ApiWebApplicationFactory? _factory;

    protected HttpClient Client { get; private set; } = null!;

    protected const string ApiV1 = "/api/v1";

    [TestInitialize]
    public void InitializeApiTest()
    {
        _factory ??= new ApiWebApplicationFactory();
        Client = _factory.CreateClient();
        ClearAuthentication();
    }

    protected void AuthenticateAs(params string[] roles) =>
        AuthenticateAs(roles, "tester@unitconverter.local");

    protected void AuthenticateAs(string[] roles, string email)
    {
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokens.Create(roles, email));
    }

    protected void ClearAuthentication() =>
        Client.DefaultRequestHeaders.Authorization = null;

    [TestCleanup]
    public void CleanupApiTest()
    {
        Client.DefaultRequestHeaders.Authorization = null;
        Client.Dispose();
    }

    [ClassCleanup]
    public void ClassCleanupApiTest()
    {
        _factory?.Dispose();
        _factory = null;
    }
}
