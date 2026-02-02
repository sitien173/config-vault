# Configuration Management Service (Vaultwarden-backed) - Design Document

## Problem Statement

Applications need a centralized, secure way to manage configuration values. Vaultwarden provides encrypted storage but lacks a developer-friendly API for configuration management. This service bridges that gap by providing a clean abstraction over Vaultwarden's Vault Management API (`bw serve`), exposing configuration as hierarchical key-value pairs.

## Constraints

- **Vaultwarden dependency**: Requires `bw serve` running and pre-unlocked externally
- **Read-only operations**: Service only reads configurations (no write/delete)
- **No caching**: Always fetches fresh data from Vaultwarden
- **Flat folder structure**: Vaultwarden folders don't support nesting; hierarchy is simulated via naming conventions

## Alternatives Considered

### Alternative 1: Direct Vaultwarden API Integration
**Approach**: Applications call Vaultwarden's `bw serve` API directly.

**Rejected because**:
- Exposes Vaultwarden API complexity to all consumers
- No abstraction for hierarchical keys
- Each application must implement its own mapping logic
- No centralized change detection

### Alternative 2: Use Existing Config Providers (Azure App Config, HashiCorp Vault)
**Approach**: Replace Vaultwarden with a dedicated configuration management system.

**Rejected because**:
- Requires additional infrastructure
- Vaultwarden already in use for secrets management
- Duplicates storage of sensitive configuration

### Alternative 3: Custom Key-Value Store
**Approach**: Build a separate database-backed configuration service.

**Rejected because**:
- Loses Vaultwarden's encryption and audit capabilities
- Additional infrastructure to maintain
- Doesn't leverage existing investment in Vaultwarden

## Final Architecture

### Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                     Consumer Applications                        │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐                       │
│  │  App 1   │  │  App 2   │  │  App 3   │                       │
│  │ (HTTP)   │  │ (Library)│  │ (HTTP)   │                       │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘                       │
└───────┼─────────────┼─────────────┼─────────────────────────────┘
        │             │             │
        ▼             ▼             ▼
┌─────────────────────────────────────────────────────────────────┐
│              Configuration Management Service                    │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                    HTTP API Layer                        │    │
│  │  ┌──────────────┐  ┌─────────────────────────────────┐  │    │
│  │  │ API Key Auth │  │ ConfigController                 │  │    │
│  │  │ Middleware   │  │ GET /config/{*key}              │  │    │
│  │  └──────────────┘  │ GET /config?prefix={ns}         │  │    │
│  │                    │ HEAD /config/{*key}             │  │    │
│  │                    │ GET /health                      │  │    │
│  │                    └─────────────────────────────────┘  │    │
│  └─────────────────────────────────────────────────────────┘    │
│                              │                                   │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                    Core Library                          │    │
│  │  ┌─────────────────────┐  ┌─────────────────────────┐   │    │
│  │  │ IConfigurationSvc   │  │ ConfigurationChangePoller│   │    │
│  │  │ - GetAsync()        │  │ - Polls at interval      │   │    │
│  │  │ - ExistsAsync()     │  │ - Fires change events    │   │    │
│  │  │ - ListAsync()       │  └─────────────────────────┘   │    │
│  │  └─────────────────────┘                                 │    │
│  │                              │                            │    │
│  │  ┌─────────────────────────────────────────────────┐    │    │
│  │  │                  VaultClient                     │    │    │
│  │  │  - Maps hierarchical keys to folders/items      │    │    │
│  │  │  - Calls Vaultwarden bw serve API               │    │    │
│  │  └─────────────────────────────────────────────────┘    │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                        Vaultwarden                               │
│  ┌─────────────────────┐  ┌─────────────────────────────────┐   │
│  │    bw serve API     │  │         Encrypted Vault          │   │
│  │  (pre-unlocked)     │──│  Folders → Namespaces            │   │
│  │                     │  │  Secure Notes → Config Items     │   │
│  └─────────────────────┘  └─────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

### Data Model

**Hierarchical Key Mapping**:
- First path segment → Vaultwarden Folder name
- Remaining path → Item `name` field
- Value → Item `notes` field
- Item type → Always `2` (Secure Note)

Example:
```
Key: "production/database/connection-string"
Value: "Server=db.example.com;Database=app;User=admin;Password=secret"

Vaultwarden Storage:
├── Folder: "production" (id: abc-123)
│   └── Secure Note Item:
│       ├── name: "database/connection-string"
│       ├── notes: "Server=db.example.com;..."
│       ├── type: 2
│       ├── folderId: "abc-123"
│       └── secureNote: { type: 0 }
```

### Library Interface

```csharp
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

public class ConfigurationChangedEventArgs : EventArgs
{
    public IReadOnlyList<string> ChangedKeys { get; init; }
    public DateTimeOffset DetectedAt { get; init; }
}
```

### HTTP API

| Method | Endpoint | Description | Response |
|--------|----------|-------------|----------|
| GET | `/config/{*key}` | Get single config | `{ "key": "...", "value": "..." }` |
| GET | `/config?prefix={ns}` | List configs in namespace | `{ "namespace": "...", "configs": {...} }` |
| HEAD | `/config/{*key}` | Check existence | 200 or 404 |
| GET | `/health` | Health check | `{ "status": "healthy", "vault": "connected" }` |

**Authentication**: API key in `X-Api-Key` header.

### Configuration

```json
{
  "ConfigVault": {
    "VaultBaseUrl": "http://localhost:8087",
    "PollingIntervalSeconds": 30,
    "ApiKeys": ["production-key-1", "production-key-2"]
  }
}
```

### Project Structure

```
src/
├── ConfigVault.Core/              # Core library (no ASP.NET dependency)
│   ├── ConfigVault.Core.csproj
│   ├── IConfigurationService.cs
│   ├── ConfigurationService.cs
│   ├── ConfigurationChangedEventArgs.cs
│   ├── Options/
│   │   └── ConfigVaultOptions.cs
│   ├── VaultClient/
│   │   ├── IVaultClient.cs
│   │   ├── VaultClient.cs
│   │   └── Models/
│   │       ├── VaultItem.cs
│   │       ├── VaultFolder.cs
│   │       └── VaultResponse.cs
│   ├── Polling/
│   │   └── ConfigurationChangePoller.cs
│   ├── Exceptions/
│   │   ├── VaultConnectionException.cs
│   │   └── VaultLockedException.cs
│   └── Extensions/
│       └── ServiceCollectionExtensions.cs
│
├── ConfigVault.Api/               # HTTP API host
│   ├── ConfigVault.Api.csproj
│   ├── Program.cs
│   ├── Controllers/
│   │   ├── ConfigController.cs
│   │   └── HealthController.cs
│   ├── Middleware/
│   │   └── ApiKeyAuthMiddleware.cs
│   ├── Models/
│   │   ├── ConfigResponse.cs
│   │   └── ConfigListResponse.cs
│   └── appsettings.json
│
└── ConfigVault.Tests/
    ├── ConfigVault.Tests.csproj
    ├── Unit/
    │   ├── ConfigurationServiceTests.cs
    │   └── VaultClientTests.cs
    └── Integration/
        └── ApiIntegrationTests.cs
```

## Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| `bw serve` becomes locked or unavailable | Medium | High | Health check endpoint reports vault status; clear error messages; consumers should handle 503 gracefully |
| Polling interval misses rapid configuration changes | Low | Medium | Configurable interval; document limitation; consumers needing real-time updates should poll directly |
| Folder naming conflicts with reserved characters | Low | Low | Validate namespace names; document naming constraints (alphanumeric, hyphens, underscores) |
| Large number of configs degrades list performance | Low | Medium | Document recommendation to partition by namespace; consider pagination in future |
| API key leaked | Medium | High | Support multiple keys for rotation; keys should be stored securely; recommend short-lived keys |

## Future Considerations (Out of Scope)

- Write operations (Set, Delete)
- Folder management (Create, Delete folders)
- Distributed caching layer
- WebSocket-based change notifications
- Multi-vault support
