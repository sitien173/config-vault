using System;
using Microsoft.Extensions.DependencyInjection;

namespace ConfigVault.Sdk.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConfigVaultClient(
        this IServiceCollection services,
        Action<ConfigVaultClientOptions> configure)
    {
        var options = new ConfigVaultClientOptions { BaseUrl = "", ApiKey = "" };
        configure(options);

        services.AddHttpClient<IConfigVaultClient, ConfigVaultClient>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = options.Timeout;
            client.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
        });

        return services;
    }
}
