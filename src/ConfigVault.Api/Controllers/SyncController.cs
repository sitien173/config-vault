using ConfigVault.Core.Exceptions;
using ConfigVault.Core.VaultClient;
using Microsoft.AspNetCore.Mvc;

namespace ConfigVault.Api.Controllers;

[ApiController]
[Route("sync")]
public class SyncController : ControllerBase
{
    private readonly IVaultClient _vaultClient;
    private readonly ILogger<SyncController> _logger;

    public SyncController(IVaultClient vaultClient, ILogger<SyncController> logger)
    {
        _vaultClient = vaultClient;
        _logger = logger;
    }

    /// <summary>
    /// Trigger an immediate sync with the upstream Vaultwarden server.
    /// </summary>
    /// <remarks>
    /// Instructs the local Bitwarden CLI (<c>bw serve</c>) to pull the latest
    /// data from the Vaultwarden instance. After a successful sync the next
    /// read of any configuration key will return the up-to-date value.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(SyncResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Sync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Sync with Vaultwarden requested");

            await _vaultClient.SyncAsync(ct);

            _logger.LogInformation("Sync with Vaultwarden completed successfully");

            return Ok(new SyncResponse
            {
                Success = true,
                SyncedAt = DateTimeOffset.UtcNow,
                Message = "Vault synced successfully"
            });
        }
        catch (VaultConnectionException ex)
        {
            _logger.LogError(ex, "Failed to sync with Vaultwarden");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Failed to sync with Vaultwarden. Ensure the vault is unlocked and reachable." });
        }
    }
}

/// <summary>Response body for a successful sync.</summary>
public sealed record SyncResponse
{
    public bool Success { get; init; }
    public DateTimeOffset SyncedAt { get; init; }
    public string Message { get; init; } = string.Empty;
}
