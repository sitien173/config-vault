use std::time::Duration;

use tokio::time::timeout;
use wiremock::matchers::{header, method, path};
use wiremock::{Mock, MockServer, ResponseTemplate};

use configvault_sdk::ConfigVaultClient;

/// Build an SSE response body with a `config-changed` event.
fn sse_body(keys: &[&str], timestamp: &str) -> String {
    let keys_json = keys
        .iter()
        .map(|k| format!("\"{}\"", k))
        .collect::<Vec<_>>()
        .join(",");
    let data = format!(
        r#"{{"keys":[{keys_json}],"timestamp":"{timestamp}"}}"#
    );
    format!("event: config-changed\ndata: {data}\n\n")
}

#[tokio::test]
async fn test_watcher_subscribe_receives_event() {
    let server = MockServer::start().await;

    let body = sse_body(&["prod/db/url"], "2024-06-01T12:00:00Z");

    Mock::given(method("GET"))
        .and(path("/events"))
        .and(header("X-Api-Key", "test-api-key"))
        .respond_with(
            ResponseTemplate::new(200)
                .set_body_string(body)
                .insert_header("Content-Type", "text/event-stream")
                .insert_header("Cache-Control", "no-cache"),
        )
        .mount(&server)
        .await;

    let client = ConfigVaultClient::new(&server.uri(), "test-api-key");
    let watcher = client.watch(None);
    let mut receiver = watcher.subscribe();

    watcher.start();

    let event = timeout(Duration::from_secs(5), receiver.recv())
        .await
        .expect("Timed out waiting for event")
        .expect("Receiver closed before receiving event");

    assert_eq!(event.keys, vec!["prod/db/url"]);
    assert_eq!(event.timestamp, "2024-06-01T12:00:00Z");
}

#[tokio::test]
async fn test_watcher_with_filter_sends_filter_param() {
    let server = MockServer::start().await;

    let body = sse_body(&["prod/cache/ttl"], "2024-06-01T13:00:00Z");

    Mock::given(method("GET"))
        .and(path("/events"))
        .and(wiremock::matchers::query_param("filter", "prod/*"))
        .and(header("X-Api-Key", "test-api-key"))
        .respond_with(
            ResponseTemplate::new(200)
                .set_body_string(body)
                .insert_header("Content-Type", "text/event-stream"),
        )
        .mount(&server)
        .await;

    let client = ConfigVaultClient::new(&server.uri(), "test-api-key");
    let watcher = client.watch(Some("prod/*"));
    let mut receiver = watcher.subscribe();

    watcher.start();

    let event = timeout(Duration::from_secs(5), receiver.recv())
        .await
        .expect("Timed out waiting for event")
        .expect("Receiver closed before receiving event");

    assert_eq!(event.keys, vec!["prod/cache/ttl"]);
}

#[tokio::test]
async fn test_watcher_subscribe_returns_broadcast_receiver() {
    let server = MockServer::start().await;

    // Two subscribers receiving the same event
    let body = sse_body(&["shared/key"], "2024-06-01T14:00:00Z");

    Mock::given(method("GET"))
        .and(path("/events"))
        .respond_with(
            ResponseTemplate::new(200)
                .set_body_string(body)
                .insert_header("Content-Type", "text/event-stream"),
        )
        .mount(&server)
        .await;

    let client = ConfigVaultClient::new(&server.uri(), "test-api-key");
    let watcher = client.watch(None);

    let mut rx1 = watcher.subscribe();
    let mut rx2 = watcher.subscribe();

    watcher.start();

    let e1 = timeout(Duration::from_secs(5), rx1.recv())
        .await
        .expect("Timed out on rx1")
        .expect("rx1 closed");

    let e2 = timeout(Duration::from_secs(5), rx2.recv())
        .await
        .expect("Timed out on rx2")
        .expect("rx2 closed");

    assert_eq!(e1.keys, vec!["shared/key"]);
    assert_eq!(e2.keys, vec!["shared/key"]);
}
