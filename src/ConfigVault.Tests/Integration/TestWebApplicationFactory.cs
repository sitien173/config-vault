using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ConfigVault.Tests.Integration;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConfigVault:PollingIntervalSeconds"] = "0",
                ["ConfigVault:ApiKeys:0"] = "dev-api-key-change-in-production",
                ["ConfigVault:VaultBaseUrl"] = "http://localhost:8087"
            };

            config.AddInMemoryCollection(settings);
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
        });
    }
}
