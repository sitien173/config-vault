namespace ConfigVault.Core;

public interface IConfigurationService
{
    /// <summary>
    /// Gets a configuration value by hierarchical key.
    /// </summary>
    /// <param name="key">Hierarchical key (e.g., "production/database/timeout")</param>
    /// <returns>The value, or null if not found</returns>
    Task<string?> GetAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Checks if a configuration key exists.
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Lists all configurations under a namespace prefix.
    /// </summary>
    /// <param name="namespacePrefix">The namespace (folder) to list</param>
    /// <returns>Dictionary of relative keys to values</returns>
    Task<IReadOnlyDictionary<string, string>> ListAsync(string namespacePrefix, CancellationToken ct = default);

    /// <summary>
    /// Event fired when configuration changes are detected.
    /// </summary>
    event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;
}
