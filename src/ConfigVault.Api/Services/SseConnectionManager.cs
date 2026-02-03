using System.Collections.Concurrent;
using System.Text.Json;
using ConfigVault.Api.Models;
using ConfigVault.Core;

namespace ConfigVault.Api.Services;

public sealed class SseConnectionManager : IDisposable
{
    private readonly ConcurrentDictionary<string, SseClient> _clients = new();
    private readonly ILogger<SseConnectionManager> _logger;
    private readonly Timer _heartbeatTimer;
    private readonly JsonSerializerOptions _jsonOptions;

    public SseConnectionManager(
        IConfigurationService configService,
        ILogger<SseConnectionManager> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        configService.ConfigurationChanged += OnConfigurationChanged;

        _heartbeatTimer = new Timer(SendHeartbeat, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    public SseClient RegisterClient(string? filterPattern)
    {
        var client = new SseClient(filterPattern);
        _clients.TryAdd(client.Id, client);
        _logger.LogInformation("SSE client {ClientId} connected with filter: {Filter}",
            client.Id, filterPattern ?? "(all)");
        return client;
    }

    public void UnregisterClient(string clientId)
    {
        if (_clients.TryRemove(clientId, out var client))
        {
            client.Dispose();
            _logger.LogInformation("SSE client {ClientId} disconnected", clientId);
        }
    }

    private void OnConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
    {
        foreach (var client in _clients.Values)
        {
            var matchingKeys = e.ChangedKeys.Where(k => client.MatchesKey(k)).ToList();
            if (matchingKeys.Count == 0)
            {
                continue;
            }

            var filteredEvent = new ConfigChangedEvent(matchingKeys, e.DetectedAt);
            var filteredJson = JsonSerializer.Serialize(filteredEvent, _jsonOptions);
            client.EventChannel.Writer.TryWrite(new SseEvent("config-changed", filteredJson));
        }

        _logger.LogDebug("Broadcast config-changed to {Count} clients", _clients.Count);
    }

    private void SendHeartbeat(object? state)
    {
        var heartbeat = new SseEvent(
            "heartbeat",
            JsonSerializer.Serialize(new { timestamp = DateTimeOffset.UtcNow }, _jsonOptions));

        foreach (var client in _clients.Values)
        {
            client.EventChannel.Writer.TryWrite(heartbeat);
        }
    }

    public void Dispose()
    {
        _heartbeatTimer.Dispose();
        foreach (var client in _clients.Values)
        {
            client.Dispose();
        }

        _clients.Clear();
    }
}
