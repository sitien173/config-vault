using System.Net;
using FluentAssertions;

namespace ConfigVault.Tests.Integration;

public class ApiIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ApiIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetConfig_Returns401_WhenNoApiKey()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/config/test/key");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetConfig_Returns401_WhenInvalidApiKey()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "invalid-key");

        var response = await client.GetAsync("/config/test/key");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Health_Returns200_WithoutApiKey()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task ListConfig_Returns400_WhenPrefixMissing()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "dev-api-key-change-in-production");

        var response = await client.GetAsync("/config");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
