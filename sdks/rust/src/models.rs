use std::collections::HashMap;

use serde::Deserialize;

/// Response from `GET /config/{key}`.
#[derive(Debug, Deserialize)]
pub struct ConfigResponse {
    pub key: String,
    pub value: String,
}

/// Response from `GET /config?prefix={namespace}`.
#[derive(Debug, Deserialize)]
pub struct ConfigListResponse {
    pub namespace: String,
    pub configs: HashMap<String, String>,
}

/// Response from `GET /health`.
#[derive(Debug, Deserialize)]
pub struct HealthResponse {
    pub status: String,
    pub vault: String,
    pub timestamp: String,
}

/// Event payload for SSE `config-changed` events.
#[derive(Debug, Clone, Deserialize)]
pub struct ConfigChangedEvent {
    pub keys: Vec<String>,
    pub timestamp: String,
}
