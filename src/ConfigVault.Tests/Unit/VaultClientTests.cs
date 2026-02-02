using ConfigVault.Core.Options;
using ConfigVault.Core.VaultClient;

namespace ConfigVault.Tests.Unit;

public class VaultClientTests
{
    [Fact]
    public void Constructor_SetsBaseAddress_FromOptions()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new ConfigVaultOptions
        {
            VaultBaseUrl = "http://localhost:9999"
        });
        var httpClient = new HttpClient();

        var _ = new VaultClient(httpClient, options);

        Assert.Equal(new Uri("http://localhost:9999/"), httpClient.BaseAddress);
    }
}
