use thiserror::Error;

/// Errors that can occur when using the ConfigVault SDK.
#[derive(Debug, Error)]
pub enum ConfigVaultError {
    /// The requested configuration key was not found.
    #[error("configuration key '{key}' not found")]
    NotFound { key: String },

    /// Authentication failed (invalid or missing API key).
    #[error("authentication failed")]
    Authentication,

    /// The ConfigVault service is unavailable.
    #[error("service unavailable")]
    ServiceUnavailable,

    /// An HTTP request error occurred.
    #[error("request failed: {0}")]
    Request(#[from] reqwest::Error),

    /// An unexpected error occurred.
    #[error("unexpected error: {message}")]
    Unexpected { status: u16, message: String },
}

/// Maps an HTTP error response to the appropriate `ConfigVaultError` variant.
pub(crate) fn handle_error_response(
    status: u16,
    key: Option<&str>,
) -> ConfigVaultError {
    match status {
        401 => ConfigVaultError::Authentication,
        404 => ConfigVaultError::NotFound {
            key: key.unwrap_or("unknown").to_string(),
        },
        503 => ConfigVaultError::ServiceUnavailable,
        code => ConfigVaultError::Unexpected {
            status: code,
            message: format!("HTTP {code}"),
        },
    }
}
