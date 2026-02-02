namespace ConfigVault.Core.VaultClient.Models;

public class VaultResponse<T>
{
    public bool Success { get; set; }

    public T? Data { get; set; }
}

public class VaultListData<T>
{
    public string Object { get; set; } = string.Empty;

    public List<T> Data { get; set; } = new();
}
