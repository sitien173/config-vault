import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { ConfigVaultClient } from '../src/client';
import {
  ConfigNotFoundError,
  AuthenticationError,
  ServiceUnavailableError,
} from '../src/errors';

describe('ConfigVaultClient', () => {
  const baseUrl = 'http://localhost:5000';
  const apiKey = 'test-api-key';
  let client: ConfigVaultClient;
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    client = new ConfigVaultClient({ baseUrl, apiKey });
    fetchMock = vi.fn();
    vi.stubGlobal('fetch', fetchMock);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  describe('get', () => {
    it('returns value when key exists', async () => {
      fetchMock.mockResolvedValue({
        ok: true,
        json: () => Promise.resolve({ key: 'prod/db/host', value: 'localhost' }),
      });

      const result = await client.get('prod/db/host');

      expect(result).toBe('localhost');
      expect(fetchMock).toHaveBeenCalledWith(
        'http://localhost:5000/config/prod/db/host',
        expect.objectContaining({
          headers: expect.objectContaining({ 'X-Api-Key': 'test-api-key' }),
        })
      );
    });

    it('throws ConfigNotFoundError when key not found', async () => {
      fetchMock.mockResolvedValue({ ok: false, status: 404 });

      await expect(client.get('unknown/key')).rejects.toThrow(ConfigNotFoundError);
    });

    it('throws AuthenticationError when unauthorized', async () => {
      fetchMock.mockResolvedValue({ ok: false, status: 401 });

      await expect(client.get('prod/key')).rejects.toThrow(AuthenticationError);
    });
  });

  describe('exists', () => {
    it('returns true when key exists', async () => {
      fetchMock.mockResolvedValue({ ok: true, status: 200 });

      const result = await client.exists('prod/db/host');

      expect(result).toBe(true);
    });

    it('returns false when key not found', async () => {
      fetchMock.mockResolvedValue({ ok: false, status: 404 });

      const result = await client.exists('unknown/key');

      expect(result).toBe(false);
    });

    it('throws ServiceUnavailableError when service unavailable', async () => {
      fetchMock.mockResolvedValue({ ok: false, status: 503 });

      await expect(client.exists('prod/key')).rejects.toThrow(ServiceUnavailableError);
    });
  });

  describe('list', () => {
    it('returns configs', async () => {
      fetchMock.mockResolvedValue({
        ok: true,
        json: () =>
          Promise.resolve({
            namespace: 'production',
            configs: { 'db/host': 'localhost', 'db/port': '5432' },
          }),
      });

      const result = await client.list('production');

      expect(result['db/host']).toBe('localhost');
    });
  });

  describe('health', () => {
    it('returns health status', async () => {
      fetchMock.mockResolvedValue({
        ok: true,
        json: () =>
          Promise.resolve({
            status: 'healthy',
            vault: 'unlocked',
            timestamp: '2026-02-02T12:00:00Z',
          }),
      });

      const result = await client.health();

      expect(result.status).toBe('healthy');
      expect(result.vault).toBe('unlocked');
    });
  });
});
