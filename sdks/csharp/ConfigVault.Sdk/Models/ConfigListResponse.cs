using System.Collections.Generic;

namespace ConfigVault.Sdk.Models;

public record ConfigListResponse(string Namespace, IReadOnlyDictionary<string, string> Configs);