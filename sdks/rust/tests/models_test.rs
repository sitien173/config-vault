use configvault_sdk::models::{ConfigChangedEvent, ConfigListResponse, ConfigResponse, HealthResponse};

// ── ConfigResponse ────────────────────────────────────────────────────────────

#[test]
fn test_config_response_deserialize() {
    let json = r#"{"key": "prod/db/url", "value": "postgres://localhost/db"}"#;
    let resp: ConfigResponse = serde_json::from_str(json).unwrap();
    assert_eq!(resp.key, "prod/db/url");
    assert_eq!(resp.value, "postgres://localhost/db");
}

#[test]
fn test_config_response_missing_field_fails() {
    let json = r#"{"key": "prod/db/url"}"#;
    let result: Result<ConfigResponse, _> = serde_json::from_str(json);
    assert!(result.is_err(), "Should fail when 'value' is missing");
}

// ── ConfigListResponse ────────────────────────────────────────────────────────

#[test]
fn test_config_list_response_deserialize() {
    let json = r#"{
        "namespace": "production",
        "configs": {
            "prod/db/url": "postgres://localhost/db",
            "prod/cache/ttl": "3600"
        }
    }"#;
    let resp: ConfigListResponse = serde_json::from_str(json).unwrap();
    assert_eq!(resp.namespace, "production");
    assert_eq!(resp.configs.len(), 2);
    assert_eq!(resp.configs["prod/db/url"], "postgres://localhost/db");
    assert_eq!(resp.configs["prod/cache/ttl"], "3600");
}

#[test]
fn test_config_list_response_empty_configs() {
    let json = r#"{"namespace": "empty-ns", "configs": {}}"#;
    let resp: ConfigListResponse = serde_json::from_str(json).unwrap();
    assert_eq!(resp.namespace, "empty-ns");
    assert!(resp.configs.is_empty());
}

// ── HealthResponse ────────────────────────────────────────────────────────────

#[test]
fn test_health_response_deserialize() {
    let json = r#"{
        "status": "healthy",
        "vault": "open",
        "timestamp": "2024-06-01T12:00:00Z"
    }"#;
    let resp: HealthResponse = serde_json::from_str(json).unwrap();
    assert_eq!(resp.status, "healthy");
    assert_eq!(resp.vault, "open");
    assert_eq!(resp.timestamp, "2024-06-01T12:00:00Z");
}

// ── ConfigChangedEvent ────────────────────────────────────────────────────────

#[test]
fn test_config_changed_event_deserialize() {
    let json = r#"{"keys": ["prod/db/url", "prod/cache/ttl"], "timestamp": "2024-06-01T12:00:00Z"}"#;
    let event: ConfigChangedEvent = serde_json::from_str(json).unwrap();
    assert_eq!(event.keys.len(), 2);
    assert_eq!(event.keys[0], "prod/db/url");
    assert_eq!(event.keys[1], "prod/cache/ttl");
    assert_eq!(event.timestamp, "2024-06-01T12:00:00Z");
}

#[test]
fn test_config_changed_event_empty_keys() {
    let json = r#"{"keys": [], "timestamp": "2024-06-01T12:00:00Z"}"#;
    let event: ConfigChangedEvent = serde_json::from_str(json).unwrap();
    assert!(event.keys.is_empty());
}

#[test]
fn test_config_changed_event_is_clone() {
    let json = r#"{"keys": ["some/key"], "timestamp": "2024-06-01T12:00:00Z"}"#;
    let event: ConfigChangedEvent = serde_json::from_str(json).unwrap();
    let cloned = event.clone();
    assert_eq!(cloned.keys, event.keys);
    assert_eq!(cloned.timestamp, event.timestamp);
}
