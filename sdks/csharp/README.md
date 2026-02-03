# ConfigVault C# SDK

C# client for the ConfigVault configuration management API.

## Installation

```bash
dotnet add package ConfigVault.Sdk
```

## Usage

### Direct Instantiation

```csharp
using ConfigVault.Sdk;

var client = new ConfigVaultClient(new ConfigVaultClientOptions
{
    BaseUrl = "http://localhost:5000",
    ApiKey = "your-api-key"
});

// Get a configuration value
var value = await client.GetAsync("production/database/connection");

// Check if key exists
var exists = await client.ExistsAsync("production/database/connection");

// List all configs in namespace
var configs = await client.ListAsync("production");

// Check service health
var health = await client.HealthAsync();
```

### Dependency Injection

```csharp
using ConfigVault.Sdk.Extensions;

builder.Services.AddConfigVaultClient(options =>
{
    options.BaseUrl = "http://localhost:5000";
    options.ApiKey = "your-api-key";
});

// Then inject IConfigVaultClient
public class MyService
{
    private readonly IConfigVaultClient _client;

    public MyService(IConfigVaultClient client) => _client = client;
}
```

## Watching for Changes

```csharp
var client = new ConfigVaultClient(options);

// Create watcher with optional filter
var watcher = client.Watch("production/*");

// Subscribe to changes
watcher.ConfigurationChanged += (sender, e) =>
{
    Console.WriteLine($"Changed keys: {string.Join(", ", e.Keys)}");
    Console.WriteLine($"Timestamp: {e.Timestamp}");
};

// Start watching
watcher.Start();

// Later, stop watching
await watcher.StopAsync();
await watcher.DisposeAsync();
```
