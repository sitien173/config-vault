"""Configuration change watcher using SSE."""

from collections.abc import AsyncIterator
from dataclasses import dataclass
from datetime import datetime
import asyncio
import json

import httpx
from httpx_sse import aconnect_sse

from configvault.exceptions import AuthenticationError, ServiceUnavailableError


@dataclass
class ConfigChangedEvent:
    """Event emitted when configuration changes are detected."""

    keys: list[str]
    timestamp: datetime


class ConfigWatcher:
    """Watches for configuration changes via SSE."""

    def __init__(
        self,
        base_url: str,
        api_key: str,
        filter_pattern: str | None = None,
        reconnect_delay: float = 5.0,
    ) -> None:
        """
        Initialize the watcher.

        Args:
            base_url: Base URL of the ConfigVault API.
            api_key: API key for authentication.
            filter_pattern: Optional glob pattern to filter keys.
            reconnect_delay: Delay before reconnecting after a failure.
        """
        self._base_url = base_url.rstrip("/")
        self._api_key = api_key
        self._filter_pattern = filter_pattern
        self._reconnect_delay = reconnect_delay
        self._running = False

    async def watch(self) -> AsyncIterator[ConfigChangedEvent]:
        """
        Watch for configuration changes.

        Yields:
            ConfigChangedEvent instances when changes are detected.
        """
        self._running = True

        while self._running:
            try:
                async for event in self._connect():
                    yield event
            except httpx.HTTPStatusError as exc:
                if exc.response.status_code == 401:
                    raise AuthenticationError() from exc
                if exc.response.status_code == 503:
                    raise ServiceUnavailableError() from exc
                raise
            except (httpx.ConnectError, httpx.ReadError):
                if self._running:
                    await asyncio.sleep(self._reconnect_delay)

    async def _connect(self) -> AsyncIterator[ConfigChangedEvent]:
        """Connect to the SSE endpoint and yield events."""
        url = f"{self._base_url}/events"
        if self._filter_pattern:
            url += f"?filter={self._filter_pattern}"

        async with httpx.AsyncClient() as client:
            async with aconnect_sse(
                client,
                "GET",
                url,
                headers={"X-Api-Key": self._api_key},
            ) as event_source:
                async for sse in event_source.aiter_sse():
                    if sse.event == "config-changed":
                        data = json.loads(sse.data)
                        timestamp = datetime.fromisoformat(
                            data["timestamp"].replace("Z", "+00:00")
                        )
                        yield ConfigChangedEvent(keys=data["keys"], timestamp=timestamp)

    def stop(self) -> None:
        """Stop watching for changes."""
        self._running = False
