using System.Threading.Tasks;
using ConfigVault.Sdk;
using FluentAssertions;
using Xunit;

namespace ConfigVault.Sdk.Tests;

public class ConfigWatcherTests
{
    [Fact]
    public void Watch_CreatesWatcherWithFilter()
    {
        var options = new ConfigVaultClientOptions
        {
            BaseUrl = "http://localhost:5000",
            ApiKey = "test-key"
        };
        var client = new ConfigVaultClient(options);

        var watcher = client.Watch("production/*");

        watcher.Should().NotBeNull();
    }

    [Fact]
    public async Task ConfigWatcher_CanStartAndStop()
    {
        var options = new ConfigVaultClientOptions
        {
            BaseUrl = "http://localhost:5000",
            ApiKey = "test-key"
        };
        var watcher = new ConfigWatcher(options);

        watcher.Start();
        await Task.Delay(100);
        await watcher.StopAsync();
    }

    [Fact]
    public async Task ConfigWatcher_DisposeStopsWatching()
    {
        var options = new ConfigVaultClientOptions
        {
            BaseUrl = "http://localhost:5000",
            ApiKey = "test-key"
        };
        var watcher = new ConfigWatcher(options);
        watcher.Start();

        await watcher.DisposeAsync();
    }
}
