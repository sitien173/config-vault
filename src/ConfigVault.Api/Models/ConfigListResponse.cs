namespace ConfigVault.Api.Models;

public class ConfigListResponse
{
    public string Namespace { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> Configs { get; init; } = new Dictionary<string, string>();
}
