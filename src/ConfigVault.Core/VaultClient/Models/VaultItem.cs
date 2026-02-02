namespace ConfigVault.Core.VaultClient.Models;

public class VaultItem
{
    public string Id { get; set; } = string.Empty;

    public string? FolderId { get; set; }

    public int Type { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public SecureNote? SecureNote { get; set; }

    public DateTimeOffset RevisionDate { get; set; }
}
