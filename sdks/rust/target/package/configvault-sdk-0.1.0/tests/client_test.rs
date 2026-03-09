use std::collections::HashMap;

use wiremock::matchers::{header, method, path, query_param};
use wiremock::{Mock, MockServer, ResponseTemplate};

use configvault_sdk::{ConfigVaultClient, ConfigVaultError};

async fn setup_client(server: &MockServer) -> ConfigVaultClient {
    ConfigVaultClient::new(&server.uri(), "test-api-key")
}

// ── get ─────────────────────────────────────────────────────────────────────

#[tokio::test]
async fn test_get_200() {
    let server = MockServer::start().await;
    Mock::given(method("GET"))
        .and(path("/config/production/db/url"))
        .and(header("X-Api-Key", "test-api-key"))
        .respond_with(ResponseTemplate::new(200).set_body_json(serde_json::json!({
            "key": "production/db/url",
            "value": "postgres://localhost/mydb"
        })))
        .mount(&server)
        .await;

    let client = setup_client(&server).await;
    let value = client.get("production/db/url").await.unwrap();
    assert_eq!(value, "postgres://localhost/mydb");
}

#[tokio::test]
async fn test_get_404() {
    let server = MockServer::start().await;
    Mock::given(method("GET"))
        .and(path("/config/missing/key"))
        .respond_with(ResponseTemplate::new(404))
        .mount(&server)
        .await;

    let client = setup_client(&server).await;
    let err = client.get("missing/key").await.unwrap_err();
    match &err {
        ConfigVaultError::NotFound { key } => assert_eq!(key, "missing/key"),
        other => panic!("Expected NotFound, got {other:?}"),
    }
}

#[tokio::test]
async fn test_get_401() {
    let server = MockServer::start().await;
    Mock::given(method("GET"))
        .and(path("/config/any/key"))
        .respond_with(ResponseTemplate::new(401))
        .mount(&server)
        .await;

    let client = setup_client(&server).await;
    let err = client.get("any/key").await.unwrap_err();
    assert!(
        matches!(err, ConfigVaultError::Authentication),
        "Expected Authentication, got {err:?}"
    );
}

#[tokio::test]
async fn test_get_503() {
    let server = MockServer::start().await;
    Mock::given(method("GET"))
        .and(path("/config/any/key"))
        .respond_with(ResponseTemplate::new(503))
        .mount(&server)
        .await;

    let client = setup_client(&server).await;
    let err = client.get("any/key").await.unwrap_err();
    assert!(
        matches!(err, ConfigVaultError::ServiceUnavailable),
        "Expected ServiceUnavailable, got {err:?}"
    );
}

// ── exists ───────────────────────────────────────────────────────────────────

#[tokio::test]
async fn test_exists_200() {
    let server = MockServer::start().await;
    Mock::given(method("HEAD"))
        .and(path("/config/exists/key"))
        .and(header("X-Api-Key", "test-api-key"))
        .respond_with(ResponseTemplate::new(200))
        .mount(&server)
        .await;

    let client = setup_client(&server).await;
    let result = client.exists("exists/key").await.unwrap();
    assert!(result);
}

#[tokio::test]
async fn test_exists_404() {
    let server = MockServer::start().await;
    Mock::given(method("HEAD"))
        .and(path("/config/missing/key"))
        .respond_with(ResponseTemplate::new(404))
        .mount(&server)
        .await;

    let client = setup_client(&server).await;
    let result = client.exists("missing/key").await.unwrap();
    assert!(!result);
}

#[tokio::test]
async fn test_exists_401() {
    let server = MockServer::start().await;
    Mock::given(method("HEAD"))
        .and(path("/config/any/key"))
        .respond_with(ResponseTemplate::new(401))
        .mount(&server)
        .await;

    let client = setup_client(&server).await;
    let err = client.exists("any/key").await.unwrap_err();
    assert!(matches!(err, ConfigVaultError::Authentication));
}

// ── list ─────────────────────────────────────────────────────────────────────

#[tokio::test]
async fn test_list_200() {
    let server = MockServer::start().await;

    let mut expected_configs: HashMap<String, String> = HashMap::new();
    expected_configs.insert("production/db/url".to_string(), "postgres://localhost/db".to_string());
    expected_configs.insert("production/db/pool".to_string(), "10".to_string());

    Mock::given(method("GET"))
        .and(path("/config"))
        .and(query_param("prefix", "production"))
        .and(header("X-Api-Key", "test-api-key"))
        .respond_with(ResponseTemplate::new(200).set_body_json(serde_json::json!({
            "namespace": "production",
            "configs": expected_configs
        })))
        .mount(&server)
        .await;

    let client = setup_client(&server).await;
    let configs = client.list("production").await.unwrap();

    assert_eq!(configs.len(), 2);
    assert_eq!(configs["production/db/url"], "postgres://localhost/db");
    assert_eq!(configs["production/db/pool"], "10");
}

#[tokio::test]
async fn test_list_401() {
    let server = MockServer::start().await;
    Mock::given(method("GET"))
        .and(path("/config"))
        .respond_with(ResponseTemplate::new(401))
        .mount(&server)
        .await;

    let client = setup_client(&server).await;
    let err = client.list("production").await.unwrap_err();
    assert!(matches!(err, ConfigVaultError::Authentication));
}

// ── health ───────────────────────────────────────────────────────────────────

#[tokio::test]
async fn test_health_200() {
    let server = MockServer::start().await;
    Mock::given(method("GET"))
        .and(path("/health"))
        .and(header("X-Api-Key", "test-api-key"))
        .respond_with(ResponseTemplate::new(200).set_body_json(serde_json::json!({
            "status": "healthy",
            "vault": "open",
            "timestamp": "2024-01-01T00:00:00Z"
        })))
        .mount(&server)
        .await;

    let client = setup_client(&server).await;
    let health = client.health().await.unwrap();

    assert_eq!(health.status, "healthy");
    assert_eq!(health.vault, "open");
    assert_eq!(health.timestamp, "2024-01-01T00:00:00Z");
}

#[tokio::test]
async fn test_health_503() {
    let server = MockServer::start().await;
    Mock::given(method("GET"))
        .and(path("/health"))
        .respond_with(ResponseTemplate::new(503).set_body_json(serde_json::json!({
            "status": "unavailable",
            "vault": "sealed",
            "timestamp": "2024-01-01T00:00:00Z"
        })))
        .mount(&server)
        .await;

    // Health endpoint returns the body regardless of status in the spec (no error mapping)
    let client = setup_client(&server).await;
    let health = client.health().await.unwrap();
    assert_eq!(health.status, "unavailable");
}

// ── url path correctness ──────────────────────────────────────────────────────

#[tokio::test]
async fn test_get_sends_correct_path_and_header() {
    let server = MockServer::start().await;
    Mock::given(method("GET"))
        .and(path("/config/namespace/sub/key"))
        .and(header("X-Api-Key", "my-secret"))
        .respond_with(ResponseTemplate::new(200).set_body_json(serde_json::json!({
            "key": "namespace/sub/key",
            "value": "val"
        })))
        .expect(1)
        .mount(&server)
        .await;

    let client = ConfigVaultClient::new(&server.uri(), "my-secret");
    client.get("namespace/sub/key").await.unwrap();
    // wiremock panics if expected call count not met after server is dropped
}

#[tokio::test]
async fn test_list_sends_prefix_query_param() {
    let server = MockServer::start().await;
    Mock::given(method("GET"))
        .and(path("/config"))
        .and(query_param("prefix", "staging"))
        .respond_with(ResponseTemplate::new(200).set_body_json(serde_json::json!({
            "namespace": "staging",
            "configs": {}
        })))
        .expect(1)
        .mount(&server)
        .await;

    let client = setup_client(&server).await;
    client.list("staging").await.unwrap();
}
