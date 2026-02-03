import {
  ConfigResponseSchema,
  ConfigListResponseSchema,
  HealthResponseSchema,
  type ConfigResponse,
  type ConfigListResponse,
  type HealthResponse,
} from './models';
import {
  ConfigVaultError,
  ConfigNotFoundError,
  AuthenticationError,
  ServiceUnavailableError,
} from './errors';

export interface ConfigVaultClientOptions {
  baseUrl: string;
  apiKey: string;
  timeout?: number;
}

export class ConfigVaultClient {
  private readonly baseUrl: string;
  private readonly apiKey: string;
  private readonly timeout: number;

  constructor(options: ConfigVaultClientOptions) {
    this.baseUrl = options.baseUrl.replace(/\/$/, '');
    this.apiKey = options.apiKey;
    this.timeout = options.timeout ?? 30000;
  }

  private async fetch(path: string, init?: RequestInit): Promise<Response> {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), this.timeout);

    try {
      const response = await fetch(`${this.baseUrl}${path}`, {
        ...init,
        headers: {
          'X-Api-Key': this.apiKey,
          ...init?.headers,
        },
        signal: controller.signal,
      });
      return response;
    } finally {
      clearTimeout(timeoutId);
    }
  }

  private handleErrorResponse(response: Response, key?: string): never {
    if (response.status === 401) {
      throw new AuthenticationError();
    }
    if (response.status === 404 && key) {
      throw new ConfigNotFoundError(key);
    }
    if (response.status === 503) {
      throw new ServiceUnavailableError();
    }
    throw new ConfigVaultError(`API error: ${response.status}`);
  }

  async get(key: string): Promise<string> {
    const response = await this.fetch(`/config/${key}`);

    if (!response.ok) {
      this.handleErrorResponse(response, key);
    }

    const data = await response.json();
    const parsed = ConfigResponseSchema.parse(data);
    return parsed.value;
  }

  async exists(key: string): Promise<boolean> {
    const response = await this.fetch(`/config/${key}`, { method: 'HEAD' });

    if (response.status === 404) {
      return false;
    }
    if (response.status === 401) {
      throw new AuthenticationError();
    }
    if (response.status === 503) {
      throw new ServiceUnavailableError();
    }

    return response.ok;
  }

  async list(namespace: string): Promise<Record<string, string>> {
    const response = await this.fetch(
      `/config?prefix=${encodeURIComponent(namespace)}`
    );

    if (!response.ok) {
      this.handleErrorResponse(response);
    }

    const data = await response.json();
    const parsed = ConfigListResponseSchema.parse(data);
    return parsed.configs;
  }

  async health(): Promise<HealthResponse> {
    const response = await this.fetch('/health');
    const data = await response.json();
    return HealthResponseSchema.parse(data);
  }
}
