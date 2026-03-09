# ConfigVault Rust SDK

Async Rust client for the [ConfigVault](https://github.com/sitien173/config-vault) configuration management API.

## Installation

Add to your `Cargo.toml`:

```toml
[dependencies]
configvault-sdk = "0.1.0"
tokio = { version = "1", features = ["rt-multi-thread", "macros"] }
```

## Usage

```rust
use configvault_sdk::ConfigVaultClient;

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let client = ConfigVaultClient::new("http://localhost:5000", "your-api-key");

    // Get a configuration value
    let value = client.get("production/database/connection").await?;
    println!("Value: {value}");

    // Check if key exists
    let exists = client.exists("production/database/connection").await?;
    println!("Exists: {exists}");

    // List all configs in a namespace
    let configs = client.list("production").await?;
    for (key, val) in &configs {
        println!("{key} = {val}");
    }

    // Check service health
    let health = client.health().await?;
    println!("Status: {}", health.status);

    Ok(())
}
```

## Watching for Changes

The watcher connects to the `/events` SSE endpoint and broadcasts `ConfigChangedEvent` to all subscribers via `tokio::sync::broadcast`.

```rust
use configvault_sdk::ConfigVaultClient;
use tokio::time::{timeout, Duration};

#[tokio::main]
async fn main() {
    let client = ConfigVaultClient::new("http://localhost:5000", "your-api-key");

    // Create a watcher with an optional namespace filter
    let watcher = client.watch(Some("production/*"));

    // Subscribe before starting (to avoid missing events)
    let mut receiver = watcher.subscribe();

    // Start the SSE loop in the background
    watcher.start();

    // Receive events
    while let Ok(event) = receiver.recv().await {
        println!("Changed keys: {:?}", event.keys);
        println!("Timestamp:    {}", event.timestamp);
    }
}
```

Multiple subscribers can call `watcher.subscribe()` independently; each receives every event.

## Error Handling

All methods return `Result<_, ConfigVaultError>`.

```rust
use configvault_sdk::{ConfigVaultClient, ConfigVaultError};

let client = ConfigVaultClient::new("http://localhost:5000", "your-api-key");

match client.get("production/db/url").await {
    Ok(value) => println!("Got: {value}"),
    Err(ConfigVaultError::NotFound { key }) => eprintln!("Key not found: {key}"),
    Err(ConfigVaultError::Authentication) => eprintln!("Invalid API key"),
    Err(ConfigVaultError::ServiceUnavailable) => eprintln!("Vault sealed or unavailable"),
    Err(ConfigVaultError::Request(e)) => eprintln!("HTTP error: {e}"),
    Err(ConfigVaultError::Unexpected { status, message }) => {
        eprintln!("Unexpected {status}: {message}")
    }
}
```

| Error variant | HTTP status | Description |
|---|---|---|
| `NotFound { key }` | 404 | Configuration key does not exist |
| `Authentication` | 401 | Invalid or missing API key |
| `ServiceUnavailable` | 503 | Vault service unavailable |
| `Request(reqwest::Error)` | — | Network or transport error |
| `Unexpected { status, message }` | other | Unmapped HTTP error |

## Custom Timeout

```rust
use configvault_sdk::ConfigVaultClient;
use std::time::Duration;

let client = ConfigVaultClient::with_timeout(
    "http://localhost:5000",
    "your-api-key",
    Duration::from_secs(10),
);
```
