---
name: Rust SDK Library
overview: Build a Rust SDK library for the ConfigVault API, matching the same public API surface as the existing TypeScript, Python, and C# SDKs -- covering client operations (get, exists, list, health), SSE-based config watching, typed error handling, and comprehensive tests.
todos:
  - id: scaffold
    content: Create sdks/rust/ directory with Cargo.toml, src/lib.rs, and module files
    status: pending
  - id: errors
    content: Implement src/errors.rs with ConfigVaultError enum using thiserror
    status: pending
  - id: models
    content: Implement src/models.rs with serde Deserialize structs
    status: pending
  - id: client
    content: Implement src/client.rs with ConfigVaultClient (get, exists, list, health, watch)
    status: pending
  - id: watcher
    content: Implement src/watcher.rs with SSE-based ConfigWatcher using reqwest-eventsource
    status: pending
  - id: lib-exports
    content: Wire up src/lib.rs to re-export public API
    status: pending
  - id: tests
    content: Write tests for client, watcher, and models using wiremock
    status: pending
  - id: readme
    content: Write README.md for the Rust SDK following existing SDK README patterns
    status: pending
  - id: parent-readme
    content: Update sdks/README.md to include the Rust SDK row
    status: pending
isProject: false
---

# Rust SDK for ConfigVault

## Directory Structure

```
sdks/rust/
  Cargo.toml
  README.md
  src/
    lib.rs          -- public re-exports
    client.rs       -- ConfigVaultClient
    watcher.rs      -- ConfigWatcher (SSE)
    models.rs       -- ConfigResponse, ConfigListResponse, HealthResponse, ConfigChangedEvent
    errors.rs       -- ConfigVaultError enum
  tests/
    client_test.rs  -- integration-style tests with mock server
    watcher_test.rs -- watcher tests
    models_test.rs  -- serde round-trip tests
```

## Dependencies (`Cargo.toml`)

- **reqwest** (with `rustls-tls` feature) -- HTTP client, async
- **tokio** -- async runtime (dev-dependency for tests; feature `rt-multi-thread`, `macros`)
- **serde** / **serde_json** -- JSON serialization
- **eventsource-stream** + **reqwest-eventsource** -- SSE client built on reqwest
- **thiserror** -- ergonomic error enum derivation
- **wiremock** (dev) -- HTTP mock server for tests

Package metadata: name `configvault-sdk`, version `0.1.0`, edition `2024`, license MIT.

## Client (`src/client.rs`)

`ConfigVaultClient` struct with builder-style construction:

```rust
pub struct ConfigVaultClient {
    base_url: String,
    api_key: String,
    http: reqwest::Client,
    timeout: Duration,
}

impl ConfigVaultClient {
    pub fn new(base_url: &str, api_key: &str) -> Self { ... }  // default 30s timeout
    pub fn with_timeout(base_url: &str, api_key: &str, timeout: Duration) -> Self { ... }

    pub async fn get(&self, key: &str) -> Result<String, ConfigVaultError> { ... }
    pub async fn exists(&self, key: &str) -> Result<bool, ConfigVaultError> { ... }
    pub async fn list(&self, namespace: &str) -> Result<HashMap<String, String>, ConfigVaultError> { ... }
    pub async fn health(&self) -> Result<HealthResponse, ConfigVaultError> { ... }
    pub fn watch(&self, filter: Option<&str>) -> ConfigWatcher { ... }
}
```

- Auth: `X-Api-Key` header on every request (except health, but we include it for simplicity as the server ignores it on `/health`).
- `get` -> `GET /config/{key}` -> returns `ConfigResponse.value`
- `exists` -> `HEAD /config/{key}` -> returns `true` on 200, `false` on 404
- `list` -> `GET /config?prefix={namespace}` -> returns `ConfigListResponse.configs` as `HashMap`
- `health` -> `GET /health` -> returns `HealthResponse`

## Error Handling (`src/errors.rs`)

```rust
#[derive(Debug, thiserror::Error)]
pub enum ConfigVaultError {
    #[error("configuration key '{key}' not found")]
    NotFound { key: String },

    #[error("authentication failed")]
    Authentication,

    #[error("service unavailable")]
    ServiceUnavailable,

    #[error("request failed: {0}")]
    Request(#[from] reqwest::Error),

    #[error("unexpected error: {message}")]
    Unexpected { status: u16, message: String },
}
```

A private `handle_error_response` helper maps HTTP status to the appropriate variant (401, 404, 503, or fallback).

## Models (`src/models.rs`)

```rust
#[derive(Debug, Deserialize)]
pub struct ConfigResponse { pub key: String, pub value: String }

#[derive(Debug, Deserialize)]
pub struct ConfigListResponse { pub namespace: String, pub configs: HashMap<String, String> }

#[derive(Debug, Deserialize)]
pub struct HealthResponse { pub status: String, pub vault: String, pub timestamp: String }

#[derive(Debug, Clone, Deserialize)]
pub struct ConfigChangedEvent { pub keys: Vec<String>, pub timestamp: String }
```

## Watcher (`src/watcher.rs`)

`ConfigWatcher` connects to `GET /events?filter={filter}` via SSE, parses `config-changed` events, and delivers them through a `tokio::sync::mpsc` channel. Reconnects automatically on error after a configurable delay (default 5 seconds).

```rust
pub struct ConfigWatcher { /* ... */ }

impl ConfigWatcher {
    pub fn subscribe(&self) -> mpsc::Receiver<ConfigChangedEvent> { ... }
    pub async fn start(&self) { ... }
    pub fn stop(&self) { ... }
}
```

Alternatively, expose `start` returning a `tokio::sync::broadcast::Receiver` to allow multiple subscribers -- matching the TypeScript `onConfigChanged` multi-handler pattern. We'll use `broadcast` for flexibility.

## Tests

- **client_test.rs**: Use `wiremock` to stand up a mock HTTP server. Test each method: `get` (200/404/401/503), `exists` (200/404), `list` (200/400), `health` (200/503). Verify correct URL paths, headers, and error mapping.
- **watcher_test.rs**: Test SSE event parsing and channel delivery with a mock SSE endpoint.
- **models_test.rs**: Verify serde deserialization of all model types from JSON strings.

## README

Follow the same structure as the other SDKs:

1. Title and description
2. Installation (`Cargo.toml` dependency)
3. Usage examples (get, exists, list, health)
4. Watching for changes
5. Error handling

## Update Parent README

Add the Rust SDK row to [sdks/README.md](sdks/README.md):

```
| Rust | `configvault-sdk` | [sdks/rust](./rust) |
```

