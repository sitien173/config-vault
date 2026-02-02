using ConfigVault.Core.Options;

namespace ConfigVault.Tests.Unit;

public class ConfigVaultOptionsTests
{
    [Fact]
    public void Defaults_AreConfiguredAsExpected()
    {
        var options = new ConfigVaultOptions();

        Assert.Equal("ConfigVault", ConfigVaultOptions.SectionName);
        Assert.Equal("http://localhost:8087", options.VaultBaseUrl);
        Assert.Equal(30, options.PollingIntervalSeconds);
        Assert.NotNull(options.ApiKeys);
        Assert.Empty(options.ApiKeys);
    }
}
