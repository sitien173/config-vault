using System.Net;

namespace ConfigVault.Tests.Integration;

public class ProgramSetupTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ProgramSetupTests(TestWebApplicationFactory factory)
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
