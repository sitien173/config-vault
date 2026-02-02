using ConfigVault.Core.Options;
using ConfigVault.Core.VaultClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConfigVault.Core.Polling;

public class ConfigurationChangePoller : BackgroundService
{
    private readonly IVaultClient _vaultClient;
    private readonly ConfigurationService _configService;
    private readonly ILogger<ConfigurationChangePoller> _logger;
    private readonly TimeSpan _pollingInterval;

    private Dictionary<string, DateTimeOffset> _itemRevisions = new();

    public ConfigurationChangePoller(
        IVaultClient vaultClient,
        IConfigurationService configService,
        IOptions<ConfigVaultOptions> options,
        ILogger<ConfigurationChangePoller> logger)
    {
        _vaultClient = vaultClient;
        _configService = (ConfigurationService)configService;
        _logger = logger;
        _pollingInterval = TimeSpan.FromSeconds(options.Value.PollingIntervalSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_pollingInterval <= TimeSpan.Zero)
        {
            _logger.LogInformation("Configuration polling is disabled");
            return;
        }

        _logger.LogInformation("Starting configuration change polling with interval {Interval}s",
            _pollingInterval.TotalSeconds);

        await LoadRevisionsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_pollingInterval, stoppingToken);

            try
            {
                await CheckForChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for configuration changes");
            }
        }
    }

    private async Task LoadRevisionsAsync(CancellationToken ct)
    {
        var folders = await _vaultClient.GetFoldersAsync(ct);
        var revisions = new Dictionary<string, DateTimeOffset>();

        foreach (var folder in folders)
        {
            var items = await _vaultClient.GetItemsByFolderIdAsync(folder.Id, ct);
            foreach (var item in items)
            {
                var key = $"{folder.Name}/{item.Name}";
                revisions[key] = item.RevisionDate;
            }
        }

        _itemRevisions = revisions;
        _logger.LogDebug("Loaded {Count} configuration revisions", revisions.Count);
    }

    private async Task CheckForChangesAsync(CancellationToken ct)
    {
        var folders = await _vaultClient.GetFoldersAsync(ct);
        var changedKeys = new List<string>();
        var newRevisions = new Dictionary<string, DateTimeOffset>();

        foreach (var folder in folders)
        {
            var items = await _vaultClient.GetItemsByFolderIdAsync(folder.Id, ct);
            foreach (var item in items)
            {
                var key = $"{folder.Name}/{item.Name}";
                newRevisions[key] = item.RevisionDate;

                if (_itemRevisions.TryGetValue(key, out var oldRevision))
                {
                    if (item.RevisionDate > oldRevision)
                    {
                        changedKeys.Add(key);
                    }
                }
                else
                {
                    changedKeys.Add(key);
                }
            }
        }

        foreach (var oldKey in _itemRevisions.Keys)
        {
            if (!newRevisions.ContainsKey(oldKey))
            {
                changedKeys.Add(oldKey);
            }
        }

        _itemRevisions = newRevisions;

        if (changedKeys.Count > 0)
        {
            _logger.LogInformation("Detected {Count} configuration changes: {Keys}",
                changedKeys.Count, string.Join(", ", changedKeys));
            _configService.RaiseConfigurationChanged(changedKeys);
        }
    }
}
