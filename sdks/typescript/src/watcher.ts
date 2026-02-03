import { AuthenticationError, ServiceUnavailableError } from './errors';

export interface ConfigChangedEvent {
  keys: string[];
  timestamp: Date;
}

export interface ConfigWatcherOptions {
  baseUrl: string;
  apiKey: string;
  filter?: string;
  reconnectDelay?: number;
}

export type ConfigChangeHandler = (event: ConfigChangedEvent) => void;

export class ConfigWatcher {
  private readonly baseUrl: string;
  private readonly apiKey: string;
  private readonly filter?: string;
  private readonly reconnectDelay: number;
  private handlers: Set<ConfigChangeHandler> = new Set();
  private running = false;

  constructor(options: ConfigWatcherOptions) {
    this.baseUrl = options.baseUrl.replace(/\/$/, '');
    this.apiKey = options.apiKey;
    this.filter = options.filter;
    this.reconnectDelay = options.reconnectDelay ?? 5000;
  }

  onConfigChanged(handler: ConfigChangeHandler): () => void {
    this.handlers.add(handler);
    return () => this.handlers.delete(handler);
  }

  start(): void {
    if (this.running) return;
    this.running = true;
    this.connect();
  }

  stop(): void {
    this.running = false;
  }

  private connect(): void {
    if (!this.running) return;

    let url = `${this.baseUrl}/events`;
    if (this.filter) {
      url += `?filter=${encodeURIComponent(this.filter)}`;
    }

    this.connectWithFetch(url);
  }

  private async connectWithFetch(url: string): Promise<void> {
    try {
      const response = await fetch(url, {
        headers: {
          'X-Api-Key': this.apiKey,
          Accept: 'text/event-stream',
        },
      });

      if (response.status === 401) {
        throw new AuthenticationError();
      }
      if (response.status === 503) {
        throw new ServiceUnavailableError();
      }
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      const reader = response.body?.getReader();
      if (!reader) return;

      const decoder = new TextDecoder();
      let buffer = '';
      let eventType = '';
      let data = '';

      while (this.running) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n');
        buffer = lines.pop() ?? '';

        for (const line of lines) {
          if (line.startsWith('event: ')) {
            eventType = line.slice(7);
          } else if (line.startsWith('data: ')) {
            data = line.slice(6);
          } else if (line === '' && eventType && data) {
            if (eventType === 'config-changed') {
              this.handleConfigChanged(data);
            }
            eventType = '';
            data = '';
          }
        }
      }
    } catch {
      if (this.running) {
        setTimeout(() => this.connect(), this.reconnectDelay);
      }
    }
  }

  private handleConfigChanged(json: string): void {
    try {
      const parsed = JSON.parse(json);
      const event: ConfigChangedEvent = {
        keys: parsed.keys,
        timestamp: new Date(parsed.timestamp),
      };

      for (const handler of this.handlers) {
        try {
          handler(event);
        } catch {
          // Ignore handler errors
        }
      }
    } catch {
      // Ignore malformed events
    }
  }
}
