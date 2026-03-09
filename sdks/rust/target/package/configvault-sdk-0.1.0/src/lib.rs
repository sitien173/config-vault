//! # ConfigVault Rust SDK
//!
//! Async Rust client for the [ConfigVault](https://github.com/sitien173/config-vault) API.
//!
//! ## Quick Start
//!
//! ```rust,no_run
//! use configvault_sdk::ConfigVaultClient;
//!
//! #[tokio::main]
//! async fn main() -> Result<(), Box<dyn std::error::Error>> {
//!     let client = ConfigVaultClient::new("http://localhost:5000", "your-api-key");
//!
//!     let value = client.get("production/database/url").await?;
//!     println!("Got: {value}");
//!
//!     let exists = client.exists("production/database/url").await?;
//!     println!("Exists: {exists}");
//!
//!     let configs = client.list("production").await?;
//!     println!("Configs: {configs:?}");
//!
//!     let health = client.health().await?;
//!     println!("Health: {}", health.status);
//!
//!     Ok(())
//! }
//! ```

pub mod client;
pub mod errors;
pub mod models;
pub mod watcher;

// Convenient re-exports of the most commonly used types
pub use client::ConfigVaultClient;
pub use errors::ConfigVaultError;
pub use models::{ConfigChangedEvent, ConfigListResponse, ConfigResponse, HealthResponse, SyncResponse};
pub use watcher::ConfigWatcher;
