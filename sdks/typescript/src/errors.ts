export class ConfigVaultError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'ConfigVaultError';
  }
}

export class ConfigNotFoundError extends ConfigVaultError {
  readonly key: string;

  constructor(key: string) {
    super(`Configuration key '${key}' not found`);
    this.name = 'ConfigNotFoundError';
    this.key = key;
  }
}

export class AuthenticationError extends ConfigVaultError {
  constructor(message = 'Invalid or missing API key') {
    super(message);
    this.name = 'AuthenticationError';
  }
}

export class ServiceUnavailableError extends ConfigVaultError {
  constructor(message = 'ConfigVault service unavailable') {
    super(message);
    this.name = 'ServiceUnavailableError';
  }
}
