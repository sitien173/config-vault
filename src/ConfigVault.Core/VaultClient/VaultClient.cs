using System.Net.Http.Json;
using System.Text.Json;
using ConfigVault.Core.Exceptions;
using ConfigVault.Core.Options;
using ConfigVault.Core.VaultClient.Models;
using Microsoft.Extensions.Options;

namespace ConfigVault.Core.VaultClient;

public class VaultClient : IVaultClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public VaultClient(HttpClient httpClient, IOptions<ConfigVaultOptions> options)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(options.Value.VaultBaseUrl.TrimEnd('/') + "/");
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<IReadOnlyList<VaultFolder>> GetFoldersAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<VaultResponse<VaultListData<VaultFolder>>>(
                "list/object/folders", _jsonOptions, ct);

            return response?.Data?.Data ?? new List<VaultFolder>();
        }
        catch (HttpRequestException ex)
        {
            throw new VaultConnectionException("Failed to connect to Vaultwarden", ex);
        }
    }

    public async Task<VaultFolder?> GetFolderByNameAsync(string name, CancellationToken ct = default)
    {
        var folders = await GetFoldersAsync(ct);
        return folders.FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<VaultItem>> GetItemsByFolderIdAsync(string folderId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<VaultResponse<VaultListData<VaultItem>>>(
                $"list/object/items?folderid={folderId}", _jsonOptions, ct);

            var items = response?.Data?.Data ?? new List<VaultItem>();
            return items.Where(i => i.Type == 2).ToList();
        }
        catch (HttpRequestException ex)
        {
            throw new VaultConnectionException("Failed to connect to Vaultwarden", ex);
        }
    }

    public async Task<VaultItem?> GetItemByIdAsync(string id, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<VaultResponse<VaultItem>>(
                $"object/item/{id}", _jsonOptions, ct);

            return response?.Success == true ? response.Data : null;
        }
        catch (HttpRequestException ex)
        {
            throw new VaultConnectionException("Failed to connect to Vaultwarden", ex);
        }
    }

    public async Task<bool> IsVaultUnlockedAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("status", ct);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            return content.Contains("\"status\":\"unlocked\"", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
