# Configuration Management Service Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a C# library and HTTP API that provides hierarchical configuration management backed by Vaultwarden's `bw serve` API.

**Architecture:** Core library (`ConfigVault.Core`) handles Vaultwarden communication and key mapping. HTTP API (`ConfigVault.Api`) wraps the library with REST endpoints and API key authentication. Change detection via background polling.

**Tech Stack:** .NET 8, ASP.NET Core, System.Text.Json, HttpClient, xUnit

---

## Task 1: Solution & Project Setup

**Files:**
- Create: `ConfigVault.sln`
- Create: `src/ConfigVault.Core/ConfigVault.Core.csproj`
- Create: `src/ConfigVault.Api/ConfigVault.Api.csproj`
- Create: `src/ConfigVault.Tests/ConfigVault.Tests.csproj`

**Step 1: Create solution and projects**

```powershell
dotnet new sln -n ConfigVault
dotnet new classlib -n ConfigVault.Core -o src/ConfigVault.Core -f net8.0
dotnet new webapi -n ConfigVault.Api -o src/ConfigVault.Api -f net8.0 --no-openapi
dotnet new xunit -n ConfigVault.Tests -o src/ConfigVault.Tests -f net8.0
```

**Step 2: Add projects to solution**

```powershell
dotnet sln add src/ConfigVault.Core/ConfigVault.Core.csproj
dotnet sln add src/ConfigVault.Api/ConfigVault.Api.csproj
dotnet sln add src/ConfigVault.Tests/ConfigVault.Tests.csproj
```

**Step 3: Add project references**

```powershell
dotnet add src/ConfigVault.Api/ConfigVault.Api.csproj reference src/ConfigVault.Core/ConfigVault.Core.csproj
dotnet add src/ConfigVault.Tests/ConfigVault.Tests.csproj reference src/ConfigVault.Core/ConfigVault.Core.csproj
dotnet add src/ConfigVault.Tests/ConfigVault.Tests.csproj reference src/ConfigVault.Api/ConfigVault.Api.csproj
```

**Step 4: Add required packages to Core**

```powershell
dotnet add src/ConfigVault.Core/ConfigVault.Core.csproj package Microsoft.Extensions.Http
dotnet add src/ConfigVault.Core/ConfigVault.Core.csproj package Microsoft.Extensions.Options
dotnet add src/ConfigVault.Core/ConfigVault.Core.csproj package Microsoft.Extensions.Hosting.Abstractions
```

**Step 5: Add test packages**

```powershell
dotnet add src/ConfigVault.Tests/ConfigVault.Tests.csproj package Moq
dotnet add src/ConfigVault.Tests/ConfigVault.Tests.csproj package FluentAssertions
dotnet add src/ConfigVault.Tests/ConfigVault.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing
```

**Step 6: Verify solution builds**

Run: `dotnet build ConfigVault.sln`
Expected: Build succeeded with 0 errors

**Step 7: Commit**

```powershell
git init
git add .
git commit -m "chore: initialize solution with Core, Api, and Tests projects"
```

---

## Task 2: Vault API Models

**Files:**
- Create: `src/ConfigVault.Core/VaultClient/Models/VaultResponse.cs`
- Create: `src/ConfigVault.Core/VaultClient/Models/VaultItem.cs`
- Create: `src/ConfigVault.Core/VaultClient/Models/VaultFolder.cs`
- Create: `src/ConfigVault.Core/VaultClient/Models/SecureNote.cs`

**Step 1: Create VaultResponse.cs**

```csharp
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
```

**Step 2: Create VaultFolder.cs**

```csharp
namespace ConfigVault.Core.VaultClient.Models;

public class VaultFolder
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
```

**Step 3: Create SecureNote.cs**

```csharp
namespace ConfigVault.Core.VaultClient.Models;

public class SecureNote
{
    public int Type { get; set; } = 0;
}
```

**Step 4: Create VaultItem.cs**

```csharp
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
```

**Step 5: Verify build**

Run: `dotnet build src/ConfigVault.Core/ConfigVault.Core.csproj`
Expected: Build succeeded

**Step 6: Commit**

```powershell
git add .
git commit -m "feat(core): add Vaultwarden API response models"
```

---

## Task 3: Configuration Options

**Files:**
- Create: `src/ConfigVault.Core/Options/ConfigVaultOptions.cs`

**Step 1: Create ConfigVaultOptions.cs**

```csharp
namespace ConfigVault.Core.Options;

public class ConfigVaultOptions
{
    public const string SectionName = "ConfigVault";
    
    /// <summary>
    /// Base URL of the bw serve API (e.g., "http://localhost:8087")
    /// </summary>
    public string VaultBaseUrl { get; set; } = "http://localhost:8087";
    
    /// <summary>
    /// Polling interval for change detection in seconds. Set to 0 to disable polling.
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 30;
    
    /// <summary>
    /// Valid API keys for HTTP API authentication.
    /// </summary>
    public List<string> ApiKeys { get; set; } = new();
}
```

**Step 2: Verify build**

Run: `dotnet build src/ConfigVault.Core/ConfigVault.Core.csproj`
Expected: Build succeeded

**Step 3: Commit**

```powershell
git add .
git commit -m "feat(core): add ConfigVaultOptions for service configuration"
```

---

## Task 4: Custom Exceptions

**Files:**
- Create: `src/ConfigVault.Core/Exceptions/VaultConnectionException.cs`
- Create: `src/ConfigVault.Core/Exceptions/VaultLockedException.cs`

**Step 1: Create VaultConnectionException.cs**

```csharp
namespace ConfigVault.Core.Exceptions;

public class VaultConnectionException : Exception
{
    public VaultConnectionException(string message) : base(message) { }
    public VaultConnectionException(string message, Exception innerException) 
        : base(message, innerException) { }
}
```

**Step 2: Create VaultLockedException.cs**

```csharp
namespace ConfigVault.Core.Exceptions;

public class VaultLockedException : Exception
{
    public VaultLockedException() 
        : base("The vault is locked. Please unlock it using 'bw unlock' before using this service.") { }
    
    public VaultLockedException(string message) : base(message) { }
}
```

**Step 3: Verify build**

Run: `dotnet build src/ConfigVault.Core/ConfigVault.Core.csproj`
Expected: Build succeeded

**Step 4: Commit**

```powershell
git add .
git commit -m "feat(core): add custom exceptions for vault errors"
```

---

## Task 5: VaultClient Interface & Implementation

**Files:**
- Create: `src/ConfigVault.Core/VaultClient/IVaultClient.cs`
- Create: `src/ConfigVault.Core/VaultClient/VaultClient.cs`
- Test: `src/ConfigVault.Tests/Unit/VaultClientTests.cs`

**Step 1: Create IVaultClient.cs**

```csharp
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
```

**Step 2: Create VaultClient.cs**

```csharp
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
            
            // Filter to secure notes only (type = 2)
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
            if (!response.IsSuccessStatusCode) return false;
            
            var content = await response.Content.ReadAsStringAsync(ct);
            // Status endpoint returns "unlocked" in the status field when vault is unlocked
            return content.Contains("\"status\":\"unlocked\"", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
```

**Step 3: Write failing test**

Create `src/ConfigVault.Tests/Unit/VaultClientTests.cs`:

```csharp
using ConfigVault.Core.Options;
using ConfigVault.Core.VaultClient;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace ConfigVault.Tests.Unit;

public class VaultClientTests
{
    [Fact]
    public void Constructor_SetsBaseAddress_FromOptions()
    {
        // Arrange
        var options = Options.Create(new ConfigVaultOptions
        {
            VaultBaseUrl = "http://localhost:9999"
        });
        var httpClient = new HttpClient();

        // Act
        var client = new VaultClient(httpClient, options);

        // Assert
        httpClient.BaseAddress.Should().Be(new Uri("http://localhost:9999/"));
    }
}
```

**Step 4: Run test**

Run: `dotnet test src/ConfigVault.Tests/ConfigVault.Tests.csproj --filter "VaultClientTests"`
Expected: PASS

**Step 5: Commit**

```powershell
git add .
git commit -m "feat(core): implement VaultClient for Vaultwarden API communication"
```

---

## Task 6: Configuration Service Interface & Implementation

**Files:**
- Create: `src/ConfigVault.Core/IConfigurationService.cs`
- Create: `src/ConfigVault.Core/ConfigurationService.cs`
- Create: `src/ConfigVault.Core/ConfigurationChangedEventArgs.cs`
- Test: `src/ConfigVault.Tests/Unit/ConfigurationServiceTests.cs`

**Step 1: Create ConfigurationChangedEventArgs.cs**

```csharp
namespace ConfigVault.Core;

public class ConfigurationChangedEventArgs : EventArgs
{
    public IReadOnlyList<string> ChangedKeys { get; init; } = Array.Empty<string>();
    public DateTimeOffset DetectedAt { get; init; } = DateTimeOffset.UtcNow;
}
```

**Step 2: Create IConfigurationService.cs**

```csharp
namespace ConfigVault.Core;

public interface IConfigurationService
{
    /// <summary>
    /// Gets a configuration value by hierarchical key.
    /// </summary>
    /// <param name="key">Hierarchical key (e.g., "production/database/timeout")</param>
    /// <returns>The value, or null if not found</returns>
    Task<string?> GetAsync(string key, CancellationToken ct = default);
    
    /// <summary>
    /// Checks if a configuration key exists.
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
    
    /// <summary>
    /// Lists all configurations under a namespace prefix.
    /// </summary>
    /// <param name="namespacePrefix">The namespace (folder) to list</param>
    /// <returns>Dictionary of relative keys to values</returns>
    Task<IReadOnlyDictionary<string, string>> ListAsync(string namespacePrefix, CancellationToken ct = default);
    
    /// <summary>
    /// Event fired when configuration changes are detected.
    /// </summary>
    event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;
}
```

**Step 3: Create ConfigurationService.cs**

```csharp
using ConfigVault.Core.VaultClient;

namespace ConfigVault.Core;

public class ConfigurationService : IConfigurationService
{
    private readonly IVaultClient _vaultClient;

    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    public ConfigurationService(IVaultClient vaultClient)
    {
        _vaultClient = vaultClient;
    }

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        
        var (folderName, itemName) = ParseKey(key);
        
        var folder = await _vaultClient.GetFolderByNameAsync(folderName, ct);
        if (folder is null) return null;
        
        var items = await _vaultClient.GetItemsByFolderIdAsync(folder.Id, ct);
        var item = items.FirstOrDefault(i => i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));
        
        return item?.Notes;
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        var value = await GetAsync(key, ct);
        return value is not null;
    }

    public async Task<IReadOnlyDictionary<string, string>> ListAsync(string namespacePrefix, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(namespacePrefix);
        
        var folder = await _vaultClient.GetFolderByNameAsync(namespacePrefix, ct);
        if (folder is null) return new Dictionary<string, string>();
        
        var items = await _vaultClient.GetItemsByFolderIdAsync(folder.Id, ct);
        
        return items
            .Where(i => i.Notes is not null)
            .ToDictionary(i => i.Name, i => i.Notes!);
    }

    internal void RaiseConfigurationChanged(IReadOnlyList<string> changedKeys)
    {
        ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs
        {
            ChangedKeys = changedKeys,
            DetectedAt = DateTimeOffset.UtcNow
        });
    }

    private static (string folderName, string itemName) ParseKey(string key)
    {
        var separatorIndex = key.IndexOf('/');
        if (separatorIndex <= 0)
        {
            throw new ArgumentException(
                "Key must contain at least one '/' separator (format: namespace/key)", nameof(key));
        }
        
        var folderName = key[..separatorIndex];
        var itemName = key[(separatorIndex + 1)..];
        
        if (string.IsNullOrWhiteSpace(itemName))
        {
            throw new ArgumentException("Item name cannot be empty", nameof(key));
        }
        
        return (folderName, itemName);
    }
}
```

**Step 4: Write tests**

Create `src/ConfigVault.Tests/Unit/ConfigurationServiceTests.cs`:

```csharp
using ConfigVault.Core;
using ConfigVault.Core.VaultClient;
using ConfigVault.Core.VaultClient.Models;
using FluentAssertions;
using Moq;

namespace ConfigVault.Tests.Unit;

public class ConfigurationServiceTests
{
    private readonly Mock<IVaultClient> _vaultClientMock;
    private readonly ConfigurationService _sut;

    public ConfigurationServiceTests()
    {
        _vaultClientMock = new Mock<IVaultClient>();
        _sut = new ConfigurationService(_vaultClientMock.Object);
    }

    [Fact]
    public async Task GetAsync_ReturnsValue_WhenKeyExists()
    {
        // Arrange
        var folder = new VaultFolder { Id = "folder-1", Name = "production" };
        var item = new VaultItem 
        { 
            Id = "item-1", 
            Name = "database/connection", 
            Notes = "Server=localhost",
            Type = 2
        };
        
        _vaultClientMock.Setup(x => x.GetFolderByNameAsync("production", It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);
        _vaultClientMock.Setup(x => x.GetItemsByFolderIdAsync("folder-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VaultItem> { item });

        // Act
        var result = await _sut.GetAsync("production/database/connection");

        // Assert
        result.Should().Be("Server=localhost");
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenFolderNotFound()
    {
        // Arrange
        _vaultClientMock.Setup(x => x.GetFolderByNameAsync("unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((VaultFolder?)null);

        // Act
        var result = await _sut.GetAsync("unknown/some/key");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenItemNotFound()
    {
        // Arrange
        var folder = new VaultFolder { Id = "folder-1", Name = "production" };
        
        _vaultClientMock.Setup(x => x.GetFolderByNameAsync("production", It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);
        _vaultClientMock.Setup(x => x.GetItemsByFolderIdAsync("folder-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VaultItem>());

        // Act
        var result = await _sut.GetAsync("production/nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("nodelimiter")]
    public async Task GetAsync_ThrowsArgumentException_ForInvalidKey(string invalidKey)
    {
        // Act
        var act = () => _sut.GetAsync(invalidKey);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ListAsync_ReturnsDictionary_WhenFolderExists()
    {
        // Arrange
        var folder = new VaultFolder { Id = "folder-1", Name = "production" };
        var items = new List<VaultItem>
        {
            new() { Name = "db/host", Notes = "localhost", Type = 2 },
            new() { Name = "db/port", Notes = "5432", Type = 2 }
        };
        
        _vaultClientMock.Setup(x => x.GetFolderByNameAsync("production", It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);
        _vaultClientMock.Setup(x => x.GetItemsByFolderIdAsync("folder-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        // Act
        var result = await _sut.ListAsync("production");

        // Assert
        result.Should().HaveCount(2);
        result["db/host"].Should().Be("localhost");
        result["db/port"].Should().Be("5432");
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue_WhenKeyExists()
    {
        // Arrange
        var folder = new VaultFolder { Id = "folder-1", Name = "production" };
        var item = new VaultItem { Name = "key", Notes = "value", Type = 2 };
        
        _vaultClientMock.Setup(x => x.GetFolderByNameAsync("production", It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);
        _vaultClientMock.Setup(x => x.GetItemsByFolderIdAsync("folder-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VaultItem> { item });

        // Act
        var result = await _sut.ExistsAsync("production/key");

        // Assert
        result.Should().BeTrue();
    }
}
```

**Step 5: Run tests**

Run: `dotnet test src/ConfigVault.Tests/ConfigVault.Tests.csproj --filter "ConfigurationServiceTests"`
Expected: All tests PASS

**Step 6: Commit**

```powershell
git add .
git commit -m "feat(core): implement ConfigurationService with hierarchical key support"
```

---

## Task 7: Change Polling Service

**Files:**
- Create: `src/ConfigVault.Core/Polling/ConfigurationChangePoller.cs`

**Step 1: Create ConfigurationChangePoller.cs**

```csharp
using ConfigVault.Core.Options;
using ConfigVault.Core.VaultClient;
using ConfigVault.Core.VaultClient.Models;
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

        // Initial load
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
                    // New key
                    changedKeys.Add(key);
                }
            }
        }

        // Check for deleted keys
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
```

**Step 2: Verify build**

Run: `dotnet build src/ConfigVault.Core/ConfigVault.Core.csproj`
Expected: Build succeeded

**Step 3: Commit**

```powershell
git add .
git commit -m "feat(core): add ConfigurationChangePoller for change detection"
```

---

## Task 8: Dependency Injection Extensions

**Files:**
- Create: `src/ConfigVault.Core/Extensions/ServiceCollectionExtensions.cs`

**Step 1: Create ServiceCollectionExtensions.cs**

```csharp
using ConfigVault.Core.Options;
using ConfigVault.Core.Polling;
using ConfigVault.Core.VaultClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConfigVault.Core.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds ConfigVault services to the service collection.
    /// </summary>
    public static IServiceCollection AddConfigVault(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ConfigVaultOptions>(
            configuration.GetSection(ConfigVaultOptions.SectionName));

        services.AddHttpClient<IVaultClient, VaultClient.VaultClient>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddHostedService<ConfigurationChangePoller>();

        return services;
    }

    /// <summary>
    /// Adds ConfigVault services with custom options.
    /// </summary>
    public static IServiceCollection AddConfigVault(
        this IServiceCollection services,
        Action<ConfigVaultOptions> configureOptions)
    {
        services.Configure(configureOptions);

        services.AddHttpClient<IVaultClient, VaultClient.VaultClient>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddHostedService<ConfigurationChangePoller>();

        return services;
    }
}
```

**Step 2: Verify build**

Run: `dotnet build src/ConfigVault.Core/ConfigVault.Core.csproj`
Expected: Build succeeded

**Step 3: Commit**

```powershell
git add .
git commit -m "feat(core): add DI extensions for easy service registration"
```

---

## Task 9: HTTP API - API Key Middleware

**Files:**
- Create: `src/ConfigVault.Api/Middleware/ApiKeyAuthMiddleware.cs`

**Step 1: Create ApiKeyAuthMiddleware.cs**

```csharp
using ConfigVault.Core.Options;
using Microsoft.Extensions.Options;

namespace ConfigVault.Api.Middleware;

public class ApiKeyAuthMiddleware
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    private readonly RequestDelegate _next;

    public ApiKeyAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IOptions<ConfigVaultOptions> options)
    {
        // Skip auth for health endpoint
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var providedKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "API key is required" });
            return;
        }

        var validKeys = options.Value.ApiKeys;
        if (validKeys.Count == 0 || !validKeys.Contains(providedKey.ToString()))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API key" });
            return;
        }

        await _next(context);
    }
}

public static class ApiKeyAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseApiKeyAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiKeyAuthMiddleware>();
    }
}
```

**Step 2: Verify build**

Run: `dotnet build src/ConfigVault.Api/ConfigVault.Api.csproj`
Expected: Build succeeded

**Step 3: Commit**

```powershell
git add .
git commit -m "feat(api): add API key authentication middleware"
```

---

## Task 10: HTTP API - Response Models

**Files:**
- Create: `src/ConfigVault.Api/Models/ConfigResponse.cs`
- Create: `src/ConfigVault.Api/Models/ConfigListResponse.cs`
- Create: `src/ConfigVault.Api/Models/HealthResponse.cs`

**Step 1: Create ConfigResponse.cs**

```csharp
namespace ConfigVault.Api.Models;

public class ConfigResponse
{
    public string Key { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}
```

**Step 2: Create ConfigListResponse.cs**

```csharp
namespace ConfigVault.Api.Models;

public class ConfigListResponse
{
    public string Namespace { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> Configs { get; init; } = new Dictionary<string, string>();
}
```

**Step 3: Create HealthResponse.cs**

```csharp
namespace ConfigVault.Api.Models;

public class HealthResponse
{
    public string Status { get; init; } = "healthy";
    public string Vault { get; init; } = "connected";
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
```

**Step 4: Verify build**

Run: `dotnet build src/ConfigVault.Api/ConfigVault.Api.csproj`
Expected: Build succeeded

**Step 5: Commit**

```powershell
git add .
git commit -m "feat(api): add HTTP response models"
```

---

## Task 11: HTTP API - Controllers

**Files:**
- Create: `src/ConfigVault.Api/Controllers/ConfigController.cs`
- Create: `src/ConfigVault.Api/Controllers/HealthController.cs`

**Step 1: Create ConfigController.cs**

```csharp
using ConfigVault.Api.Models;
using ConfigVault.Core;
using ConfigVault.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ConfigVault.Api.Controllers;

[ApiController]
[Route("config")]
public class ConfigController : ControllerBase
{
    private readonly IConfigurationService _configService;
    private readonly ILogger<ConfigController> _logger;

    public ConfigController(IConfigurationService configService, ILogger<ConfigController> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    /// <summary>
    /// Get a configuration value by hierarchical key.
    /// </summary>
    [HttpGet("{*key}")]
    [ProducesResponseType(typeof(ConfigResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(string key, CancellationToken ct)
    {
        try
        {
            var value = await _configService.GetAsync(key, ct);
            
            if (value is null)
            {
                return NotFound(new { error = $"Configuration key '{key}' not found" });
            }

            return Ok(new ConfigResponse { Key = key, Value = value });
        }
        catch (VaultConnectionException ex)
        {
            _logger.LogError(ex, "Failed to connect to vault");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, 
                new { error = "Vault service unavailable" });
        }
        catch (VaultLockedException ex)
        {
            _logger.LogError(ex, "Vault is locked");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, 
                new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Check if a configuration key exists.
    /// </summary>
    [HttpHead("{*key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Exists(string key, CancellationToken ct)
    {
        try
        {
            var exists = await _configService.ExistsAsync(key, ct);
            return exists ? Ok() : NotFound();
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
        catch (VaultConnectionException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    /// <summary>
    /// List all configurations under a namespace.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ConfigListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> List([FromQuery] string prefix, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return BadRequest(new { error = "Query parameter 'prefix' is required" });
        }

        try
        {
            var configs = await _configService.ListAsync(prefix, ct);
            
            return Ok(new ConfigListResponse
            {
                Namespace = prefix,
                Configs = configs
            });
        }
        catch (VaultConnectionException ex)
        {
            _logger.LogError(ex, "Failed to connect to vault");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, 
                new { error = "Vault service unavailable" });
        }
    }
}
```

**Step 2: Create HealthController.cs**

```csharp
using ConfigVault.Api.Models;
using ConfigVault.Core.VaultClient;
using Microsoft.AspNetCore.Mvc;

namespace ConfigVault.Api.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly IVaultClient _vaultClient;

    public HealthController(IVaultClient vaultClient)
    {
        _vaultClient = vaultClient;
    }

    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var isUnlocked = await _vaultClient.IsVaultUnlockedAsync(ct);

        var response = new HealthResponse
        {
            Status = isUnlocked ? "healthy" : "unhealthy",
            Vault = isUnlocked ? "unlocked" : "locked or unavailable",
            Timestamp = DateTimeOffset.UtcNow
        };

        return isUnlocked ? Ok(response) : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }
}
```

**Step 3: Verify build**

Run: `dotnet build src/ConfigVault.Api/ConfigVault.Api.csproj`
Expected: Build succeeded

**Step 4: Commit**

```powershell
git add .
git commit -m "feat(api): add Config and Health controllers"
```

---

## Task 12: HTTP API - Program.cs Setup

**Files:**
- Modify: `src/ConfigVault.Api/Program.cs`
- Modify: `src/ConfigVault.Api/appsettings.json`

**Step 1: Update Program.cs**

Replace contents of `src/ConfigVault.Api/Program.cs`:

```csharp
using ConfigVault.Api.Middleware;
using ConfigVault.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddConfigVault(builder.Configuration);

var app = builder.Build();

app.UseApiKeyAuth();
app.MapControllers();

app.Run();

// Make Program accessible for integration tests
public partial class Program { }
```

**Step 2: Update appsettings.json**

Replace contents of `src/ConfigVault.Api/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConfigVault": {
    "VaultBaseUrl": "http://localhost:8087",
    "PollingIntervalSeconds": 30,
    "ApiKeys": ["dev-api-key-change-in-production"]
  }
}
```

**Step 3: Delete auto-generated files**

Delete any auto-generated files like `WeatherForecast.cs` or `Controllers/WeatherForecastController.cs` if they exist.

**Step 4: Verify build and run**

Run: `dotnet build src/ConfigVault.Api/ConfigVault.Api.csproj`
Expected: Build succeeded

**Step 5: Commit**

```powershell
git add .
git commit -m "feat(api): configure Program.cs and appsettings"
```

---

## Task 13: Integration Tests

**Files:**
- Create: `src/ConfigVault.Tests/Integration/ApiIntegrationTests.cs`

**Step 1: Create ApiIntegrationTests.cs**

```csharp
using System.Net;
using System.Net.Http.Json;
using ConfigVault.Api.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ConfigVault.Tests.Integration;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetConfig_Returns401_WhenNoApiKey()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/config/test/key");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetConfig_Returns401_WhenInvalidApiKey()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "invalid-key");

        // Act
        var response = await client.GetAsync("/config/test/key");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Health_Returns200_WithoutApiKey()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        // Health endpoint should be accessible without API key
        // Status depends on whether Vaultwarden is running
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task ListConfig_Returns400_WhenPrefixMissing()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "dev-api-key-change-in-production");

        // Act
        var response = await client.GetAsync("/config");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
```

**Step 2: Run integration tests**

Run: `dotnet test src/ConfigVault.Tests/ConfigVault.Tests.csproj --filter "ApiIntegrationTests"`
Expected: All tests PASS

**Step 3: Commit**

```powershell
git add .
git commit -m "test: add API integration tests"
```

---

## Task 14: Final Cleanup & Documentation

**Files:**
- Delete: `src/ConfigVault.Core/Class1.cs` (if exists)
- Create: `README.md`

**Step 1: Delete placeholder files**

Delete any `Class1.cs` files generated by `dotnet new`.

**Step 2: Create README.md**

```markdown
# ConfigVault

Configuration Management Service backed by Vaultwarden.

## Prerequisites

- .NET 8.0 SDK
- Vaultwarden with `bw serve` running and unlocked

## Quick Start

### As a Library

```csharp
// Program.cs
builder.Services.AddConfigVault(builder.Configuration);

// Usage
public class MyService
{
    private readonly IConfigurationService _config;
    
    public MyService(IConfigurationService config)
    {
        _config = config;
    }
    
    public async Task DoSomething()
    {
        var connectionString = await _config.GetAsync("production/database/connection");
        var allDbConfigs = await _config.ListAsync("production");
    }
}
```

### As HTTP API

```bash
# Start the API
cd src/ConfigVault.Api
dotnet run

# Get a config value
curl -H "X-Api-Key: your-key" http://localhost:5000/config/production/database/connection

# List configs in namespace
curl -H "X-Api-Key: your-key" "http://localhost:5000/config?prefix=production"

# Check health
curl http://localhost:5000/health
```

## Configuration

```json
{
  "ConfigVault": {
    "VaultBaseUrl": "http://localhost:8087",
    "PollingIntervalSeconds": 30,
    "ApiKeys": ["your-api-key"]
  }
}
```

## Key Format

Keys follow hierarchical format: `namespace/path/to/key`

- First segment = Vaultwarden folder name
- Remaining path = Item name in folder
- Value stored in Secure Note's `notes` field

Example: `production/database/connection-string` maps to:
- Folder: `production`
- Item name: `database/connection-string`
```

**Step 3: Run all tests**

Run: `dotnet test ConfigVault.sln`
Expected: All tests PASS

**Step 4: Final commit**

```powershell
git add .
git commit -m "docs: add README and cleanup placeholder files"
```

---

## Verification Checklist

After completing all tasks, verify:

- [ ] `dotnet build ConfigVault.sln` succeeds with no errors
- [ ] `dotnet test ConfigVault.sln` passes all tests
- [ ] API starts with `dotnet run --project src/ConfigVault.Api`
- [ ] Health endpoint responds at `http://localhost:5000/health`
- [ ] API key authentication works (401 without key, 200 with valid key)
