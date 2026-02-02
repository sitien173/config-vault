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
