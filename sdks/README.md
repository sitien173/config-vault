# ConfigVault SDKs

Client SDKs for the ConfigVault configuration management API.

## Available SDKs

| Language | Package | Directory |
|----------|---------|-----------|
| Python | `configvault` | [sdks/python](./python) |
| C# | `ConfigVault.Sdk` | [sdks/csharp](./csharp) |
| TypeScript | `@configvault/sdk` | [sdks/typescript](./typescript) |

## Common Interface

All SDKs provide the same core methods:

| Method | Description |
|--------|-------------|
| `get(key)` | Get a configuration value by hierarchical key |
| `exists(key)` | Check if a configuration key exists |
| `list(namespace)` | List all configurations in a namespace |
| `health()` | Check the health of the ConfigVault service |

## Authentication

All SDKs require an API key passed via constructor options. The key is sent in the `X-Api-Key` header.

## Error Handling

All SDKs throw equivalent exceptions:

| Exception | HTTP Status | Description |
|-----------|-------------|-------------|
| `ConfigNotFoundError` | 404 | Key does not exist |
| `AuthenticationError` | 401 | Invalid or missing API key |
| `ServiceUnavailableError` | 503 | Vault service unavailable |

## Key Format

Keys follow hierarchical format: `namespace/path/to/key`

Example: `production/database/connection-string`
