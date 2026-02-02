using ConfigVault.Core;
using ConfigVault.Core.Options;
using ConfigVault.Core.Polling;
using ConfigVault.Core.VaultClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace ConfigVault.Tests.Unit;

public class ConfigurationChangePollerTests
{
    [Fact]
    public async Task ExecuteAsync_DoesNotQueryVault_WhenPollingDisabled()
    {
        var vaultClientMock = new Mock<IVaultClient>(MockBehavior.Strict);
        var configService = new ConfigurationService(Mock.Of<IVaultClient>());
        var options = Options.Create(new ConfigVaultOptions { PollingIntervalSeconds = 0 });
        var logger = Mock.Of<ILogger<ConfigurationChangePoller>>();
        var sut = new TestableConfigurationChangePoller(
            vaultClientMock.Object,
            configService,
            options,
            logger);

        await sut.RunAsync(CancellationToken.None);

        vaultClientMock.VerifyNoOtherCalls();
    }

    private sealed class TestableConfigurationChangePoller : ConfigurationChangePoller
    {
        public TestableConfigurationChangePoller(
            IVaultClient vaultClient,
            IConfigurationService configService,
            IOptions<ConfigVaultOptions> options,
            ILogger<ConfigurationChangePoller> logger)
            : base(vaultClient, configService, options, logger)
        {
        }

        public Task RunAsync(CancellationToken ct)
        {
            return ExecuteAsync(ct);
        }
    }
}
