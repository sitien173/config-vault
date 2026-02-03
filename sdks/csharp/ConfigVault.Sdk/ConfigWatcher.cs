using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ConfigVault.Sdk.Exceptions;

namespace ConfigVault.Sdk;

public class ConfigWatcher : IAsyncDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string? _filterPattern;
    private readonly TimeSpan _reconnectDelay;
    private CancellationTokenSource? _cts;
    private Task? _watchTask;

    public event EventHandler<ConfigChangedEventArgs>? ConfigurationChanged;

    public ConfigWatcher(ConfigVaultClientOptions options, string? filterPattern = null)
    {
        _baseUrl = options.BaseUrl.TrimEnd('/');
        _filterPattern = filterPattern;
        _reconnectDelay = TimeSpan.FromSeconds(5);
        _httpClient = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        _httpClient.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
    }

    public void Start()
    {
        if (_watchTask != null)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _watchTask = WatchLoopAsync(_cts.Token);
    }

    public async Task StopAsync()
    {
        if (_cts == null || _watchTask == null)
        {
            return;
        }

        _cts.Cancel();
        try
        {
            await _watchTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            _watchTask = null;
        }
    }

    private async Task WatchLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ConnectAndReadAsync(ct).ConfigureAwait(false);
            }
            catch (HttpRequestException)
            {
                if (!ct.IsCancellationRequested)
                {
                    await Task.Delay(_reconnectDelay, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ConnectAndReadAsync(CancellationToken ct)
    {
        var url = $"{_baseUrl}/events";
        if (!string.IsNullOrEmpty(_filterPattern))
        {
            url += $"?filter={Uri.EscapeDataString(_filterPattern)}";
        }

        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new AuthenticationException();
        }

        if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            throw new ServiceUnavailableException();
        }

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        string? eventType = null;
        string? data = null;

        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
            if (line == null)
            {
                break;
            }

            if (line.StartsWith("event: ", StringComparison.Ordinal))
            {
                eventType = line[7..];
            }
            else if (line.StartsWith("data: ", StringComparison.Ordinal))
            {
                data = line[6..];
            }
            else if (string.IsNullOrEmpty(line) && eventType != null && data != null)
            {
                if (eventType == "config-changed")
                {
                    ProcessConfigChangedEvent(data);
                }

                eventType = null;
                data = null;
            }
        }
    }

    private void ProcessConfigChangedEvent(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var keys = new List<string>();
            foreach (var element in root.GetProperty("keys").EnumerateArray())
            {
                var value = element.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    keys.Add(value);
                }
            }

            var timestamp = root.GetProperty("timestamp").GetDateTimeOffset();

            ConfigurationChanged?.Invoke(this, new ConfigChangedEventArgs
            {
                Keys = keys,
                Timestamp = timestamp
            });
        }
        catch (JsonException)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _httpClient.Dispose();
    }
}
