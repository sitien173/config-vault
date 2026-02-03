# ConfigVault Python SDK

Python client for the ConfigVault configuration management API.

## Installation

```bash
pip install configvault
```

## Usage

```python
from configvault import ConfigVaultClient

client = ConfigVaultClient(
    base_url="http://localhost:5000",
    api_key="your-api-key"
)

# Get a configuration value
value = await client.get("production/database/connection")

# Check if key exists
exists = await client.exists("production/database/connection")

# List all configs in namespace
configs = await client.list("production")

# Check service health
health = await client.health()
```