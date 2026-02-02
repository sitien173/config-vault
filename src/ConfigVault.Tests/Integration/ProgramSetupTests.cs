using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ConfigVault.Tests.Integration;

public class ProgramSetupTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProgramSetupTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ConfigEndpoint_RequiresApiKey()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/config/production/key");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
