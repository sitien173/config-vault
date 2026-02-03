import { describe, it, expect, vi } from 'vitest';
import { ConfigWatcher } from '../src/watcher';

describe('ConfigWatcher', () => {
  const baseUrl = 'http://localhost:5000';
  const apiKey = 'test-api-key';

  describe('constructor', () => {
    it('creates watcher with options', () => {
      const watcher = new ConfigWatcher({ baseUrl, apiKey, filter: 'production/*' });

      expect(watcher).toBeDefined();
    });
  });

  describe('onConfigChanged', () => {
    it('adds handler and returns unsubscribe function', () => {
      const watcher = new ConfigWatcher({ baseUrl, apiKey });
      const handler = vi.fn();

      const unsubscribe = watcher.onConfigChanged(handler);

      expect(typeof unsubscribe).toBe('function');
    });

    it('unsubscribe removes handler', () => {
      const watcher = new ConfigWatcher({ baseUrl, apiKey });
      const handler = vi.fn();

      const unsubscribe = watcher.onConfigChanged(handler);
      unsubscribe();
    });
  });

  describe('start/stop', () => {
    it('can start and stop without error', () => {
      const watcher = new ConfigWatcher({ baseUrl, apiKey });

      watcher.start();
      watcher.stop();
    });

    it('start is idempotent', () => {
      const watcher = new ConfigWatcher({ baseUrl, apiKey });

      watcher.start();
      watcher.start();
      watcher.stop();
    });
  });
});
