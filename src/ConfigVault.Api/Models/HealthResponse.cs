namespace ConfigVault.Api.Models;

public class HealthResponse
{
    public string Status { get; init; } = "healthy";

    public string Vault { get; init; } = "connected";

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
