"""Tests for ConfigWatcher."""

from datetime import datetime, timezone

from configvault import ConfigVaultClient
from configvault.watcher import ConfigChangedEvent, ConfigWatcher


class TestConfigWatcher:
    def test_creates_with_filter(self) -> None:
        watcher = ConfigWatcher(
            base_url="http://localhost:5000",
            api_key="test-key",
            filter_pattern="production/*",
        )

        assert watcher._filter_pattern == "production/*"
        assert watcher._running is False

    def test_stop_sets_running_false(self) -> None:
        watcher = ConfigWatcher(
            base_url="http://localhost:5000",
            api_key="test-key",
        )
        watcher._running = True

        watcher.stop()

        assert watcher._running is False

    def test_client_watch_returns_watcher(self) -> None:
        client = ConfigVaultClient("http://localhost:5000", "test-key")

        watcher = client.watch("production/*")

        assert isinstance(watcher, ConfigWatcher)
        assert watcher._filter_pattern == "production/*"


class TestConfigChangedEvent:
    def test_creates_event(self) -> None:
        event = ConfigChangedEvent(
            keys=["prod/db/host", "prod/db/port"],
            timestamp=datetime(2026, 2, 2, 12, 0, 0, tzinfo=timezone.utc),
        )

        assert len(event.keys) == 2
        assert "prod/db/host" in event.keys
