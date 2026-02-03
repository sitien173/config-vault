"""Tests for ConfigVault client."""

import httpx
import pytest
import respx

from configvault import (
    AuthenticationError,
    ConfigNotFoundError,
    ConfigVaultClient,
    ServiceUnavailableError,
)


@pytest.fixture
def base_url() -> str:
    return "http://localhost:5000"


@pytest.fixture
def api_key() -> str:
    return "test-api-key"


class TestConfigVaultClient:
    @respx.mock
    async def test_get_returns_value(self, base_url: str, api_key: str) -> None:
        respx.get(f"{base_url}/config/prod/db/host").mock(
            return_value=httpx.Response(
                200,
                json={"key": "prod/db/host", "value": "localhost"},
            )
        )

        async with ConfigVaultClient(base_url, api_key) as client:
            value = await client.get("prod/db/host")

        assert value == "localhost"

    @respx.mock
    async def test_get_raises_not_found(self, base_url: str, api_key: str) -> None:
        respx.get(f"{base_url}/config/unknown/key").mock(
            return_value=httpx.Response(404, json={"error": "Not found"})
        )

        async with ConfigVaultClient(base_url, api_key) as client:
            with pytest.raises(ConfigNotFoundError) as exc:
                await client.get("unknown/key")

        assert exc.value.key == "unknown/key"

    @respx.mock
    async def test_get_raises_auth_error(self, base_url: str, api_key: str) -> None:
        respx.get(f"{base_url}/config/prod/key").mock(
            return_value=httpx.Response(401, json={"error": "Unauthorized"})
        )

        async with ConfigVaultClient(base_url, api_key) as client:
            with pytest.raises(AuthenticationError):
                await client.get("prod/key")

    @respx.mock
    async def test_exists_returns_true(self, base_url: str, api_key: str) -> None:
        respx.head(f"{base_url}/config/prod/db/host").mock(
            return_value=httpx.Response(200)
        )

        async with ConfigVaultClient(base_url, api_key) as client:
            exists = await client.exists("prod/db/host")

        assert exists is True

    @respx.mock
    async def test_exists_returns_false(self, base_url: str, api_key: str) -> None:
        respx.head(f"{base_url}/config/unknown/key").mock(
            return_value=httpx.Response(404)
        )

        async with ConfigVaultClient(base_url, api_key) as client:
            exists = await client.exists("unknown/key")

        assert exists is False

    @respx.mock
    async def test_list_returns_configs(self, base_url: str, api_key: str) -> None:
        respx.get(f"{base_url}/config", params={"prefix": "production"}).mock(
            return_value=httpx.Response(
                200,
                json={
                    "namespace": "production",
                    "configs": {"db/host": "localhost", "db/port": "5432"},
                },
            )
        )

        async with ConfigVaultClient(base_url, api_key) as client:
            configs = await client.list("production")

        assert configs["db/host"] == "localhost"
        assert configs["db/port"] == "5432"

    @respx.mock
    async def test_health_returns_status(self, base_url: str, api_key: str) -> None:
        respx.get(f"{base_url}/health").mock(
            return_value=httpx.Response(
                200,
                json={
                    "status": "healthy",
                    "vault": "unlocked",
                    "timestamp": "2026-02-02T12:00:00Z",
                },
            )
        )

        async with ConfigVaultClient(base_url, api_key) as client:
            health = await client.health()

        assert health.status == "healthy"
        assert health.vault == "unlocked"

    @respx.mock
    async def test_service_unavailable(self, base_url: str, api_key: str) -> None:
        respx.get(f"{base_url}/config/prod/key").mock(
            return_value=httpx.Response(503, json={"error": "Service unavailable"})
        )

        async with ConfigVaultClient(base_url, api_key) as client:
            with pytest.raises(ServiceUnavailableError):
                await client.get("prod/key")