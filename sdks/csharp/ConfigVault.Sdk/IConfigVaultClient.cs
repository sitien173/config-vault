using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ConfigVault.Sdk.Models;

namespace ConfigVault.Sdk;

public interface IConfigVaultClient : IDisposable
{
    Task<string> GetAsync(string key, CancellationToken ct = default);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, string>> ListAsync(string namespacePrefix, CancellationToken ct = default);
    Task<HealthResponse> HealthAsync(CancellationToken ct = default);
    ConfigWatcher Watch(string? filterPattern = null);
}
