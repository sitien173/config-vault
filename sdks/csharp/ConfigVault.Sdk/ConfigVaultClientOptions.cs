using System;

namespace ConfigVault.Sdk;

public class ConfigVaultClientOptions
{
    public required string BaseUrl { get; set; }
    public required string ApiKey { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
