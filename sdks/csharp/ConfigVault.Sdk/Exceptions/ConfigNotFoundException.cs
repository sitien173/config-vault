namespace ConfigVault.Sdk.Exceptions;

public class ConfigNotFoundException : ConfigVaultException
{
    public string Key { get; }

    public ConfigNotFoundException(string key) : base($"Configuration key '{key}' not found")
    {
        Key = key;
    }
}