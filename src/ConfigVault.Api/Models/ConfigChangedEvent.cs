using System.Text.Json.Serialization;

namespace ConfigVault.Api.Models;

public record ConfigChangedEvent(
    [property: JsonPropertyName("keys")] IReadOnlyList<string> Keys,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp
);
