"""ConfigVault SDK models."""

from datetime import datetime

from pydantic import BaseModel, Field


class ConfigResponse(BaseModel):
    """Response model for a single configuration value."""

    key: str
    value: str


class ConfigListResponse(BaseModel):
    """Response model for listing configurations."""

    namespace: str = Field(alias="namespace")
    configs: dict[str, str]


class HealthResponse(BaseModel):
    """Response model for health check."""

    status: str
    vault: str
    timestamp: datetime