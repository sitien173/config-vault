using System;
using ConfigVault.Sdk.Extensions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ConfigVault.Sdk.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddConfigVaultClient_RegistersClient()
    {
        var services = new ServiceCollection();

        services.AddConfigVaultClient(options =>
        {
            options.BaseUrl = "http://localhost:5000";
            options.ApiKey = "test-key";
        });

        using var provider = services.BuildServiceProvider();

        var client = provider.GetService<IConfigVaultClient>();

        client.Should().NotBeNull();
    }
}
