use std::collections::HashMap;
use std::time::Duration;

use reqwest::header::{HeaderMap, HeaderValue};

use crate::errors::{handle_error_response, ConfigVaultError};
use crate::models::{ConfigListResponse, ConfigResponse, HealthResponse, SyncResponse};
use crate::watcher::ConfigWatcher;

/// Async HTTP client for the ConfigVault API.
pub struct ConfigVaultClient {
    base_url: String,
    api_key: String,
    http: reqwest::Client,
    timeout: Duration,
}

impl ConfigVaultClient {
    /// Create a new client with the default 30-second timeout.
    pub fn new(base_url: &str, api_key: &str) -> Self {
        Self::with_timeout(base_url, api_key, Duration::from_secs(30))
    }

    /// Create a new client with a custom timeout.
    pub fn with_timeout(base_url: &str, api_key: &str, timeout: Duration) -> Self {
        let mut headers = HeaderMap::new();
        headers.insert(
            "X-Api-Key",
            HeaderValue::from_str(api_key).expect("API key must be a valid header value"),
        );

        let http = reqwest::Client::builder()
            .default_headers(headers)
            .timeout(timeout)
            .use_rustls_tls()
            .build()
            .expect("Failed to build HTTP client");

        Self {
            base_url: base_url.trim_end_matches('/').to_string(),
            api_key: api_key.to_string(),
            http,
            timeout,
        }
    }

    /// Get a configuration value by key.
    ///
    /// Sends `GET /config/{key}` and returns the value string.
    pub async fn get(&self, key: &str) -> Result<String, ConfigVaultError> {
        let url = format!("{}/config/{}", self.base_url, key);
        let response = self.http.get(&url).send().await?;

        if !response.status().is_success() {
            return Err(handle_error_response(
                response.status().as_u16(),
                Some(key),
            ));
        }

        let data: ConfigResponse = response.json().await?;
        Ok(data.value)
    }

    /// Check whether a configuration key exists.
    ///
    /// Sends `HEAD /config/{key}` and returns `true` on 200, `false` on 404.
    pub async fn exists(&self, key: &str) -> Result<bool, ConfigVaultError> {
        let url = format!("{}/config/{}", self.base_url, key);
        let response = self.http.head(&url).send().await?;

        let status = response.status().as_u16();
        match status {
            200 => Ok(true),
            404 => Ok(false),
            401 | 503 => Err(handle_error_response(status, None)),
            _ => Err(handle_error_response(status, None)),
        }
    }

    /// List all configurations under a namespace prefix.
    ///
    /// Sends `GET /config?prefix={namespace}` and returns a map of key → value.
    pub async fn list(&self, namespace: &str) -> Result<HashMap<String, String>, ConfigVaultError> {
        let url = reqwest::Url::parse_with_params(
            &format!("{}/config", self.base_url),
            &[("prefix", namespace)],
        )
        .map_err(|e| ConfigVaultError::Unexpected {
            status: 0,
            message: format!("Invalid URL: {e}"),
        })?;
        let response = self.http.get(url).send().await?;

        if !response.status().is_success() {
            return Err(handle_error_response(
                response.status().as_u16(),
                None,
            ));
        }

        let data: ConfigListResponse = response.json().await?;
        Ok(data.configs)
    }

    /// Trigger an immediate sync with the upstream Vaultwarden server.
    ///
    /// Sends `POST /sync` and instructs ConfigVault to pull the latest data
    /// from Vaultwarden. After a successful sync the next read of any
    /// configuration key will return the up-to-date value.
    pub async fn sync(&self) -> Result<SyncResponse, ConfigVaultError> {
        let url = format!("{}/sync", self.base_url);
        let response = self.http.post(&url).send().await?;

        if !response.status().is_success() {
            return Err(handle_error_response(response.status().as_u16(), None));
        }

        let data: SyncResponse = response.json().await?;
        Ok(data)
    }

    /// Check the health of the ConfigVault service.
    ///
    /// Sends `GET /health` and returns the health response.
    pub async fn health(&self) -> Result<HealthResponse, ConfigVaultError> {
        let url = format!("{}/health", self.base_url);
        let response = self.http.get(&url).send().await?;

        let data: HealthResponse = response.json().await?;
        Ok(data)
    }

    /// Create a `ConfigWatcher` for SSE-based change notifications.
    ///
    /// The watcher connects to `GET /events?filter={filter}`.
    pub fn watch(&self, filter: Option<&str>) -> ConfigWatcher {
        ConfigWatcher::new(
            &self.base_url,
            &self.api_key,
            filter,
            self.timeout,
        )
    }
}
