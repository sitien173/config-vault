using System.Net;
using FluentAssertions;

namespace ConfigVault.Tests.Integration;

public class SseIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public SseIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EventsEndpoint_Returns401_WhenNoApiKey()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/events");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task EventsEndpoint_ReturnsEventStream_WithValidApiKey()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "dev-api-key-change-in-production");

        var request = new HttpRequestMessage(HttpMethod.Get, "/events");
        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/event-stream");
    }

    [Fact]
    public async Task EventsEndpoint_AcceptsFilterParameter()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "dev-api-key-change-in-production");

        var request = new HttpRequestMessage(HttpMethod.Get, "/events?filter=production/*");
        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
