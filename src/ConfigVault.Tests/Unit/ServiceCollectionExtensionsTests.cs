using ConfigVault.Core;
using ConfigVault.Core.Extensions;
using ConfigVault.Core.Options;
using ConfigVault.Core.Polling;
using ConfigVault.Core.VaultClient;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ConfigVault.Tests.Unit;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddConfigVault_RegistersExpectedServices_FromConfiguration()
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConfigVault:VaultBaseUrl"] = "http://localhost:7777",
            ["ConfigVault:PollingIntervalSeconds"] = "5"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
        var services = new ServiceCollection();

        services.AddConfigVault(configuration);

        var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>();

        provider.GetService<IConfigurationService>().Should().NotBeNull();
        provider.GetService<IVaultClient>().Should().NotBeNull();
        hostedServices.Should().ContainSingle(x => x is ConfigurationChangePoller);
    }

    [Fact]
    public void AddConfigVault_ConfiguresOptions_UsingDelegate()
    {
        var services = new ServiceCollection();

        services.AddConfigVault(options =>
        {
            options.VaultBaseUrl = "http://localhost:9000";
            options.PollingIntervalSeconds = 45;
            options.ApiKeys = new List<string> { "key-a" };
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ConfigVaultOptions>>().Value;

        options.VaultBaseUrl.Should().Be("http://localhost:9000");
        options.PollingIntervalSeconds.Should().Be(45);
        options.ApiKeys.Should().ContainSingle("key-a");
    }
}
