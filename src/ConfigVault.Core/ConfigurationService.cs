using ConfigVault.Core.VaultClient;

namespace ConfigVault.Core;

public class ConfigurationService : IConfigurationService
{
    private readonly IVaultClient _vaultClient;

    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    public ConfigurationService(IVaultClient vaultClient)
    {
        _vaultClient = vaultClient;
    }

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var (folderName, itemName) = ParseKey(key);

        var folder = await _vaultClient.GetFolderByNameAsync(folderName, ct);
        if (folder is null)
        {
            return null;
        }

        var items = await _vaultClient.GetItemsByFolderIdAsync(folder.Id, ct);
        var item = items.FirstOrDefault(i => i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));

        return item?.Notes;
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        var value = await GetAsync(key, ct);
        return value is not null;
    }

    public async Task<IReadOnlyDictionary<string, string>> ListAsync(string namespacePrefix, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(namespacePrefix);

        var folder = await _vaultClient.GetFolderByNameAsync(namespacePrefix, ct);
        if (folder is null)
        {
            return new Dictionary<string, string>();
        }

        var items = await _vaultClient.GetItemsByFolderIdAsync(folder.Id, ct);

        return items
            .Where(i => i.Notes is not null)
            .ToDictionary(i => i.Name, i => i.Notes!);
    }

    internal void RaiseConfigurationChanged(IReadOnlyList<string> changedKeys)
    {
        ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs
        {
            ChangedKeys = changedKeys,
            DetectedAt = DateTimeOffset.UtcNow
        });
    }

    private static (string folderName, string itemName) ParseKey(string key)
    {
        var separatorIndex = key.IndexOf('/');
        if (separatorIndex <= 0)
        {
            throw new ArgumentException(
                "Key must contain at least one '/' separator (format: namespace/key)", nameof(key));
        }

        var folderName = key[..separatorIndex];
        var itemName = key[(separatorIndex + 1)..];

        if (string.IsNullOrWhiteSpace(itemName))
        {
            throw new ArgumentException("Item name cannot be empty", nameof(key));
        }

        return (folderName, itemName);
    }
}
