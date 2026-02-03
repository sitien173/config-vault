"""Tests for ConfigVault models and exceptions."""

from datetime import datetime, timezone

import pytest

from configvault.exceptions import (
    AuthenticationError,
    ConfigNotFoundError,
    ServiceUnavailableError,
)
from configvault.models import ConfigResponse, ConfigListResponse, HealthResponse


class TestConfigResponse:
    def test_create_from_dict(self) -> None:
        data = {"key": "prod/db/host", "value": "localhost"}
        response = ConfigResponse.model_validate(data)

        assert response.key == "prod/db/host"
        assert response.value == "localhost"


class TestConfigListResponse:
    def test_create_from_dict(self) -> None:
        data = {
            "namespace": "production",
            "configs": {"db/host": "localhost", "db/port": "5432"},
        }
        response = ConfigListResponse.model_validate(data)

        assert response.namespace == "production"
        assert response.configs["db/host"] == "localhost"
        assert len(response.configs) == 2


class TestHealthResponse:
    def test_create_from_dict(self) -> None:
        data = {
            "status": "healthy",
            "vault": "unlocked",
            "timestamp": "2026-02-02T12:00:00Z",
        }
        response = HealthResponse.model_validate(data)

        assert response.status == "healthy"
        assert response.vault == "unlocked"

    def test_timestamp_parses_to_datetime(self) -> None:
        data = {
            "status": "healthy",
            "vault": "unlocked",
            "timestamp": "2026-02-02T12:00:00Z",
        }
        response = HealthResponse.model_validate(data)

        assert isinstance(response.timestamp, datetime)
        assert response.timestamp.tzinfo is not None
        assert response.timestamp == datetime(2026, 2, 2, 12, 0, 0, tzinfo=timezone.utc)


class TestExceptions:
    def test_config_not_found_error_message(self) -> None:
        error = ConfigNotFoundError("prod/missing")

        assert str(error) == "Configuration key 'prod/missing' not found"

    def test_authentication_error_default_message(self) -> None:
        error = AuthenticationError()

        assert str(error) == "Invalid or missing API key"

    def test_service_unavailable_error_default_message(self) -> None:
        error = ServiceUnavailableError()

        assert str(error) == "ConfigVault service unavailable"