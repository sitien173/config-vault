"""ConfigVault Python SDK."""

from configvault.client import ConfigVaultClient
from configvault.models import ConfigResponse, ConfigListResponse, HealthResponse
from configvault.exceptions import (
    ConfigVaultError,
    ConfigNotFoundError,
    AuthenticationError,
    ServiceUnavailableError,
)
from configvault.watcher import ConfigWatcher, ConfigChangedEvent

__version__ = "0.1.0"
__all__ = [
    "ConfigVaultClient",
    "ConfigResponse",
    "ConfigListResponse",
    "HealthResponse",
    "ConfigVaultError",
    "ConfigNotFoundError",
    "AuthenticationError",
    "ServiceUnavailableError",
    "ConfigWatcher",
    "ConfigChangedEvent",
]
