using ConfigVault.Core.Options;
using ConfigVault.Core.Polling;
using ConfigVault.Core.VaultClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConfigVault.Core.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds ConfigVault services to the service collection.
    /// </summary>
    public static IServiceCollection AddConfigVault(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ConfigVaultOptions>(
            configuration.GetSection(ConfigVaultOptions.SectionName));

        services.AddHttpClient<IVaultClient, VaultClient.VaultClient>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddHostedService<ConfigurationChangePoller>();

        return services;
    }

    /// <summary>
    /// Adds ConfigVault services with custom options.
    /// </summary>
    public static IServiceCollection AddConfigVault(
        this IServiceCollection services,
        Action<ConfigVaultOptions> configureOptions)
    {
        services.Configure(configureOptions);

        services.AddHttpClient<IVaultClient, VaultClient.VaultClient>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddHostedService<ConfigurationChangePoller>();

        return services;
    }
}
