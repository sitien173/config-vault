"""ConfigVault API client."""

from typing import Optional

import httpx

from configvault.exceptions import (
    AuthenticationError,
    ConfigNotFoundError,
    ConfigVaultError,
    ServiceUnavailableError,
)
from configvault.models import ConfigListResponse, ConfigResponse, HealthResponse


class ConfigVaultClient:
    """Async client for ConfigVault API."""

    def __init__(
        self,
        base_url: str,
        api_key: str,
        timeout: float = 30.0,
    ) -> None:
        """
        Initialize the ConfigVault client.

        Args:
            base_url: Base URL of the ConfigVault API (e.g., "http://localhost:5000")
            api_key: API key for authentication
            timeout: Request timeout in seconds
        """
        self._base_url = base_url.rstrip("/")
        self._api_key = api_key
        self._timeout = timeout
        self._client: Optional[httpx.AsyncClient] = None

    async def _get_client(self) -> httpx.AsyncClient:
        """Get or create the HTTP client."""
        if self._client is None or self._client.is_closed:
            self._client = httpx.AsyncClient(
                base_url=self._base_url,
                headers={"X-Api-Key": self._api_key},
                timeout=self._timeout,
            )
        return self._client

    async def close(self) -> None:
        """Close the HTTP client."""
        if self._client is not None:
            await self._client.aclose()
            self._client = None

    async def __aenter__(self) -> "ConfigVaultClient":
        """Async context manager entry."""
        return self

    async def __aexit__(self, *args) -> None:
        """Async context manager exit."""
        await self.close()

    def watch(self, filter_pattern: str | None = None) -> "ConfigWatcher":
        """
        Create a watcher for configuration changes.

        Args:
            filter_pattern: Optional glob pattern to filter keys (e.g., "production/*")

        Returns:
            ConfigWatcher instance for async iteration
        """
        from configvault.watcher import ConfigWatcher

        return ConfigWatcher(
            base_url=self._base_url,
            api_key=self._api_key,
            filter_pattern=filter_pattern,
        )

    def _handle_error_response(self, response: httpx.Response, key: Optional[str] = None) -> None:
        """Handle error responses from the API."""
        if response.status_code == 401:
            raise AuthenticationError()
        if response.status_code == 404 and key:
            raise ConfigNotFoundError(key)
        if response.status_code == 503:
            raise ServiceUnavailableError()
        if response.status_code >= 400:
            raise ConfigVaultError(f"API error: {response.status_code}")

    async def get(self, key: str) -> str:
        """
        Get a configuration value by key.

        Args:
            key: Hierarchical key (e.g., "production/database/connection")

        Returns:
            The configuration value

        Raises:
            ConfigNotFoundError: If the key does not exist
            AuthenticationError: If the API key is invalid
            ServiceUnavailableError: If the service is unavailable
        """
        client = await self._get_client()
        response = await client.get(f"/config/{key}")

        self._handle_error_response(response, key)

        data = ConfigResponse.model_validate(response.json())
        return data.value

    async def exists(self, key: str) -> bool:
        """
        Check if a configuration key exists.

        Args:
            key: Hierarchical key to check

        Returns:
            True if the key exists, False otherwise
        """
        client = await self._get_client()
        response = await client.head(f"/config/{key}")

        if response.status_code == 404:
            return False
        if response.status_code == 401:
            raise AuthenticationError()
        if response.status_code == 503:
            raise ServiceUnavailableError()

        return response.status_code == 200

    async def list(self, namespace: str) -> dict[str, str]:
        """
        List all configurations in a namespace.

        Args:
            namespace: The namespace (folder) to list

        Returns:
            Dictionary mapping keys to values
        """
        client = await self._get_client()
        response = await client.get("/config", params={"prefix": namespace})

        self._handle_error_response(response)

        data = ConfigListResponse.model_validate(response.json())
        return data.configs

    async def health(self) -> HealthResponse:
        """
        Check the health of the ConfigVault service.

        Returns:
            Health status information
        """
        client = await self._get_client()
        # Health endpoint doesn't require API key, but we send it anyway
        response = await client.get("/health")

        return HealthResponse.model_validate(response.json())
