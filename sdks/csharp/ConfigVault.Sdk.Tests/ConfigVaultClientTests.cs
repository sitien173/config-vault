using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using ConfigVault.Sdk.Exceptions;
using ConfigVault.Sdk.Models;
using FluentAssertions;
using RichardSzalay.MockHttp;
using Xunit;

namespace ConfigVault.Sdk.Tests;

public class ConfigVaultClientTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly ConfigVaultClient _client;

    public ConfigVaultClientTests()
    {
        _mockHttp = new MockHttpMessageHandler();
        var httpClient = _mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("http://localhost:5000/");
        httpClient.DefaultRequestHeaders.Add("X-Api-Key", "test-key");
        _client = new ConfigVaultClient(httpClient);
    }

    [Fact]
    public async Task GetAsync_ReturnsValue_WhenKeyExists()
    {
        _mockHttp.When("http://localhost:5000/config/prod/db/host")
            .Respond("application/json", JsonSerializer.Serialize(new { key = "prod/db/host", value = "localhost" }));

        var result = await _client.GetAsync("prod/db/host");

        result.Should().Be("localhost");
    }

    [Fact]
    public async Task GetAsync_ThrowsConfigNotFoundException_WhenKeyNotFound()
    {
        _mockHttp.When("http://localhost:5000/config/unknown/key")
            .Respond(HttpStatusCode.NotFound);

        var act = () => _client.GetAsync("unknown/key");

        await act.Should().ThrowAsync<ConfigNotFoundException>()
            .Where(e => e.Key == "unknown/key");
    }

    [Fact]
    public async Task GetAsync_ThrowsAuthenticationException_WhenUnauthorized()
    {
        _mockHttp.When("http://localhost:5000/config/prod/key")
            .Respond(HttpStatusCode.Unauthorized);

        var act = () => _client.GetAsync("prod/key");

        await act.Should().ThrowAsync<AuthenticationException>();
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue_WhenKeyExists()
    {
        _mockHttp.When(HttpMethod.Head, "http://localhost:5000/config/prod/db/host")
            .Respond(HttpStatusCode.OK);

        var result = await _client.ExistsAsync("prod/db/host");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_WhenKeyNotFound()
    {
        _mockHttp.When(HttpMethod.Head, "http://localhost:5000/config/unknown/key")
            .Respond(HttpStatusCode.NotFound);

        var result = await _client.ExistsAsync("unknown/key");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ListAsync_ReturnsConfigs()
    {
        _mockHttp.When("http://localhost:5000/config?prefix=production")
            .Respond("application/json", JsonSerializer.Serialize(new
            {
                @namespace = "production",
                configs = new Dictionary<string, string>
                {
                    ["db/host"] = "localhost",
                    ["db/port"] = "5432"
                }
            }));

        var result = await _client.ListAsync("production");

        result.Should().HaveCount(2);
        result["db/host"].Should().Be("localhost");
    }

    [Fact]
    public async Task HealthAsync_ReturnsStatus()
    {
        _mockHttp.When("http://localhost:5000/health")
            .Respond("application/json", JsonSerializer.Serialize(new
            {
                status = "healthy",
                vault = "unlocked",
                timestamp = "2026-02-02T12:00:00Z"
            }));

        var result = await _client.HealthAsync();

        result.Status.Should().Be("healthy");
        result.Vault.Should().Be("unlocked");
    }

    [Fact]
    public async Task GetAsync_ThrowsServiceUnavailableException_When503()
    {
        _mockHttp.When("http://localhost:5000/config/prod/key")
            .Respond(HttpStatusCode.ServiceUnavailable);

        var act = () => _client.GetAsync("prod/key");

        await act.Should().ThrowAsync<ServiceUnavailableException>();
    }

    public void Dispose()
    {
        _client.Dispose();
        _mockHttp.Dispose();
    }
}
