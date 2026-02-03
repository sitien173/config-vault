# ConfigVault TypeScript SDK

TypeScript/JavaScript client for the ConfigVault configuration management API.

## Installation

```bash
npm install @configvault/sdk
```

## Usage

```typescript
import { ConfigVaultClient } from '@configvault/sdk';

const client = new ConfigVaultClient({
  baseUrl: 'http://localhost:5000',
  apiKey: 'your-api-key',
});

// Get a configuration value
const value = await client.get('production/database/connection');

// Check if key exists
const exists = await client.exists('production/database/connection');

// List all configs in namespace
const configs = await client.list('production');

// Check service health
const health = await client.health();
```
