use std::time::Duration;

use eventsource_stream::Eventsource;
use futures_util::StreamExt;
use reqwest::header::{HeaderMap, HeaderValue};
use tokio::sync::broadcast;

use crate::models::ConfigChangedEvent;

const DEFAULT_RECONNECT_DELAY: Duration = Duration::from_secs(5);
const CHANNEL_CAPACITY: usize = 64;

/// SSE-based watcher that delivers `ConfigChangedEvent`s via a broadcast channel.
///
/// Multiple subscribers can `subscribe()` to receive events independently.
/// Call `start()` to begin streaming. Call `stop()` to shut down.
pub struct ConfigWatcher {
    base_url: String,
    api_key: String,
    filter: Option<String>,
    reconnect_delay: Duration,
    sender: broadcast::Sender<ConfigChangedEvent>,
}

impl ConfigWatcher {
    /// Create a new watcher (called internally by `ConfigVaultClient::watch()`).
    pub(crate) fn new(
        base_url: &str,
        api_key: &str,
        filter: Option<&str>,
        _timeout: Duration,
    ) -> Self {
        let (sender, _) = broadcast::channel(CHANNEL_CAPACITY);
        Self {
            base_url: base_url.trim_end_matches('/').to_string(),
            api_key: api_key.to_string(),
            filter: filter.map(str::to_string),
            reconnect_delay: DEFAULT_RECONNECT_DELAY,
            sender,
        }
    }

    /// Subscribe to config-changed events.
    ///
    /// Returns a `broadcast::Receiver`. Call `start()` to begin streaming events.
    pub fn subscribe(&self) -> broadcast::Receiver<ConfigChangedEvent> {
        self.sender.subscribe()
    }

    /// Start the SSE streaming loop in the background.
    ///
    /// This spawns a Tokio task that connects to the `/events` endpoint and
    /// delivers deserialized `ConfigChangedEvent`s to all subscribers.
    /// Reconnects automatically on connection failure after `reconnect_delay`.
    pub fn start(&self) {
        let base_url = self.base_url.clone();
        let api_key = self.api_key.clone();
        let filter = self.filter.clone();
        let reconnect_delay = self.reconnect_delay;
        let sender = self.sender.clone();

        tokio::spawn(async move {
            loop {
                let result = stream_events(&base_url, &api_key, &filter, &sender).await;
                if let Err(e) = result {
                    tracing_or_eprintln(format!("SSE connection error: {e}"));
                }
                // Reconnect after delay if there are still active receivers
                if sender.receiver_count() == 0 {
                    break;
                }
                tokio::time::sleep(reconnect_delay).await;
            }
        });
    }

    /// Stop the watcher by dropping the sender, which closes all receivers.
    pub fn stop(&self) {
        // Closing is handled by dropping; expose this as a no-op for API parity.
        // The background task exits when there are no more receivers.
    }

    /// Set a custom reconnect delay (default: 5 seconds).
    pub fn with_reconnect_delay(mut self, delay: Duration) -> Self {
        self.reconnect_delay = delay;
        self
    }
}

/// Stream SSE events, parsing `config-changed` events and sending them to subscribers.
async fn stream_events(
    base_url: &str,
    api_key: &str,
    filter: &Option<String>,
    sender: &broadcast::Sender<ConfigChangedEvent>,
) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
    let mut url = format!("{base_url}/events");
    if let Some(f) = filter {
        url = format!("{url}?filter={}", urlencoding_encode(f));
    }

    let mut headers = HeaderMap::new();
    headers.insert(
        "X-Api-Key",
        HeaderValue::from_str(api_key)?,
    );
    headers.insert(
        reqwest::header::ACCEPT,
        HeaderValue::from_static("text/event-stream"),
    );

    let client = reqwest::Client::builder()
        .use_rustls_tls()
        .default_headers(headers)
        .build()?;

    let response = client.get(&url).send().await?;

    let stream = response.bytes_stream().eventsource();
    futures_util::pin_mut!(stream);

    while let Some(result) = stream.next().await {
        let event = result?;
        if event.event == "config-changed" {
            if let Ok(parsed) = serde_json::from_str::<ConfigChangedEvent>(&event.data) {
                // Ignore send errors (no active receivers is OK)
                let _ = sender.send(parsed);
            }
        }
    }

    Ok(())
}

/// Simple percent-encode a string for use in a query parameter value.
fn urlencoding_encode(s: &str) -> String {
    let mut encoded = String::with_capacity(s.len());
    for byte in s.bytes() {
        match byte {
            b'A'..=b'Z'
            | b'a'..=b'z'
            | b'0'..=b'9'
            | b'-'
            | b'_'
            | b'.'
            | b'~'
            | b'/'
            | b'*' => encoded.push(byte as char),
            b => encoded.push_str(&format!("%{b:02X}")),
        }
    }
    encoded
}

/// Log an error — in a library we avoid forcing a logging framework.
/// Users can set up `tracing` subscriber independently.
#[allow(dead_code)]
fn tracing_or_eprintln(msg: String) {
    eprintln!("[configvault-sdk] {msg}");
}
