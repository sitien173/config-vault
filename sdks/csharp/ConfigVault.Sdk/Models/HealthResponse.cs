using System;

namespace ConfigVault.Sdk.Models;

public record HealthResponse(string Status, string Vault, DateTimeOffset Timestamp);