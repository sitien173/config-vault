using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ConfigVault.Sdk.Exceptions;
using ConfigVault.Sdk.Models;

namespace ConfigVault.Sdk;

public class ConfigVaultClient : IConfigVaultClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    public ConfigVaultClient(ConfigVaultClientOptions options)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/"),
            Timeout = options.Timeout
        };
        _httpClient.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public ConfigVaultClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<string> GetAsync(string key, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"config/{key}", ct).ConfigureAwait(false);
        HandleErrorResponse(response, key);

        var result = await response.Content.ReadFromJsonAsync<ConfigResponse>(_jsonOptions, ct).ConfigureAwait(false);
        return result?.Value ?? throw new ConfigVaultException("Invalid response from server");
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Head, $"config/{key}");
        var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return false;
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new AuthenticationException();
        if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            throw new ServiceUnavailableException();

        return response.IsSuccessStatusCode;
    }

    public async Task<IReadOnlyDictionary<string, string>> ListAsync(string namespacePrefix, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"config?prefix={Uri.EscapeDataString(namespacePrefix)}", ct).ConfigureAwait(false);
        HandleErrorResponse(response);

        var result = await response.Content.ReadFromJsonAsync<ConfigListResponse>(_jsonOptions, ct).ConfigureAwait(false);
        return result?.Configs ?? new Dictionary<string, string>();
    }

    public async Task<HealthResponse> HealthAsync(CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync("health", ct).ConfigureAwait(false);
        var result = await response.Content.ReadFromJsonAsync<HealthResponse>(_jsonOptions, ct).ConfigureAwait(false);
        return result ?? throw new ConfigVaultException("Invalid health response from server");
    }

    private static void HandleErrorResponse(HttpResponseMessage response, string? key = null)
    {
        if (response.IsSuccessStatusCode) return;

        throw response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new AuthenticationException(),
            HttpStatusCode.NotFound when key != null => new ConfigNotFoundException(key),
            HttpStatusCode.ServiceUnavailable => new ServiceUnavailableException(),
            _ => new ConfigVaultException($"API error: {(int)response.StatusCode}")
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _httpClient.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
