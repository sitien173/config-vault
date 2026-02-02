namespace ConfigVault.Core.Options;

public class ConfigVaultOptions
{
    public const string SectionName = "ConfigVault";

    /// <summary>
    /// Base URL of the bw serve API (e.g., "http://localhost:8087")
    /// </summary>
    public string VaultBaseUrl { get; set; } = "http://localhost:8087";

    /// <summary>
    /// Polling interval for change detection in seconds. Set to 0 to disable polling.
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Valid API keys for HTTP API authentication.
    /// </summary>
    public List<string> ApiKeys { get; set; } = new();
}
