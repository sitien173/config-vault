using System.Text;
using ConfigVault.Api.Middleware;
using ConfigVault.Core.Options;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ConfigVault.Tests.Unit;

public class ApiKeyAuthMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_BypassesAuth_ForHealthEndpoint()
    {
        var nextCalled = false;
        var middleware = new ApiKeyAuthMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Path = "/health";

        await middleware.InvokeAsync(context, Options.Create(new ConfigVaultOptions()));

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ReturnsUnauthorized_WhenApiKeyMissing()
    {
        var middleware = new ApiKeyAuthMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Request.Path = "/config/production/key";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context, Options.Create(new ConfigVaultOptions { ApiKeys = ["valid"] }));

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        var body = await ReadBodyAsync(context.Response.Body);
        body.Should().Contain("API key is required");
    }

    [Fact]
    public async Task InvokeAsync_ReturnsUnauthorized_WhenApiKeyInvalid()
    {
        var middleware = new ApiKeyAuthMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Request.Path = "/config/production/key";
        context.Request.Headers["X-Api-Key"] = "wrong";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context, Options.Create(new ConfigVaultOptions { ApiKeys = ["valid"] }));

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        var body = await ReadBodyAsync(context.Response.Body);
        body.Should().Contain("Invalid API key");
    }

    [Fact]
    public async Task InvokeAsync_CallsNext_WhenApiKeyValid()
    {
        var nextCalled = false;
        var middleware = new ApiKeyAuthMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Path = "/config/production/key";
        context.Request.Headers["X-Api-Key"] = "valid";

        await middleware.InvokeAsync(context, Options.Create(new ConfigVaultOptions { ApiKeys = ["valid"] }));

        nextCalled.Should().BeTrue();
    }

    private static async Task<string> ReadBodyAsync(Stream stream)
    {
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}
