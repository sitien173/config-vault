import { describe, it, expect } from 'vitest';
import {
  ConfigResponseSchema,
  ConfigListResponseSchema,
  HealthResponseSchema,
} from '../src/models';
import {
  ConfigVaultError,
  ConfigNotFoundError,
  AuthenticationError,
  ServiceUnavailableError,
} from '../src/errors';

describe('ConfigResponseSchema', () => {
  it('parses valid response', () => {
    const data = { key: 'prod/db/host', value: 'localhost' };
    const result = ConfigResponseSchema.parse(data);

    expect(result.key).toBe('prod/db/host');
    expect(result.value).toBe('localhost');
  });
});

describe('ConfigListResponseSchema', () => {
  it('parses valid response', () => {
    const data = {
      namespace: 'production',
      configs: { 'db/host': 'localhost', 'db/port': '5432' },
    };
    const result = ConfigListResponseSchema.parse(data);

    expect(result.namespace).toBe('production');
    expect(result.configs['db/host']).toBe('localhost');
  });
});

describe('HealthResponseSchema', () => {
  it('parses valid response with timestamp', () => {
    const data = {
      status: 'healthy',
      vault: 'unlocked',
      timestamp: '2026-02-02T12:00:00Z',
    };
    const result = HealthResponseSchema.parse(data);

    expect(result.status).toBe('healthy');
    expect(result.vault).toBe('unlocked');
    expect(result.timestamp).toBeInstanceOf(Date);
  });
});

describe('ConfigVaultError', () => {
  it('sets name and message', () => {
    const error = new ConfigVaultError('Base error');

    expect(error.name).toBe('ConfigVaultError');
    expect(error.message).toBe('Base error');
  });
});

describe('ConfigNotFoundError', () => {
  it('includes key in message', () => {
    const error = new ConfigNotFoundError('prod/missing');

    expect(error.name).toBe('ConfigNotFoundError');
    expect(error.key).toBe('prod/missing');
    expect(error.message).toBe("Configuration key 'prod/missing' not found");
  });
});

describe('AuthenticationError', () => {
  it('uses default message', () => {
    const error = new AuthenticationError();

    expect(error.name).toBe('AuthenticationError');
    expect(error.message).toBe('Invalid or missing API key');
  });
});

describe('ServiceUnavailableError', () => {
  it('uses default message', () => {
    const error = new ServiceUnavailableError();

    expect(error.name).toBe('ServiceUnavailableError');
    expect(error.message).toBe('ConfigVault service unavailable');
  });
});
