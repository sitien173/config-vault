using ConfigVault.Core.VaultClient.Models;

namespace ConfigVault.Core.VaultClient;

public interface IVaultClient
{
    Task<IReadOnlyList<VaultFolder>> GetFoldersAsync(CancellationToken ct = default);

    Task<VaultFolder?> GetFolderByNameAsync(string name, CancellationToken ct = default);

    Task<IReadOnlyList<VaultItem>> GetItemsByFolderIdAsync(string folderId, CancellationToken ct = default);

    Task<VaultItem?> GetItemByIdAsync(string id, CancellationToken ct = default);

    Task<bool> IsVaultUnlockedAsync(CancellationToken ct = default);
}
