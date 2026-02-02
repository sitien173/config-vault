using ConfigVault.Api.Models;
using ConfigVault.Core.VaultClient;
using Microsoft.AspNetCore.Mvc;

namespace ConfigVault.Api.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly IVaultClient _vaultClient;

    public HealthController(IVaultClient vaultClient)
    {
        _vaultClient = vaultClient;
    }

    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var isUnlocked = await _vaultClient.IsVaultUnlockedAsync(ct);

        var response = new HealthResponse
        {
            Status = isUnlocked ? "healthy" : "unhealthy",
            Vault = isUnlocked ? "unlocked" : "locked or unavailable",
            Timestamp = DateTimeOffset.UtcNow
        };

        return isUnlocked ? Ok(response) : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }
}
