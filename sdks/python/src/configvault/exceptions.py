"""ConfigVault SDK exceptions."""


class ConfigVaultError(Exception):
    """Base exception for ConfigVault SDK."""

    pass


class ConfigNotFoundError(ConfigVaultError):
    """Raised when a configuration key is not found."""

    def __init__(self, key: str) -> None:
        self.key = key
        super().__init__(f"Configuration key '{key}' not found")


class AuthenticationError(ConfigVaultError):
    """Raised when API key authentication fails."""

    def __init__(self, message: str = "Invalid or missing API key") -> None:
        super().__init__(message)


class ServiceUnavailableError(ConfigVaultError):
    """Raised when the ConfigVault service is unavailable."""

    def __init__(self, message: str = "ConfigVault service unavailable") -> None:
        super().__init__(message)