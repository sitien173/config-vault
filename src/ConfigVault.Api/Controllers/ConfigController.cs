using ConfigVault.Api.Models;
using ConfigVault.Core;
using ConfigVault.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ConfigVault.Api.Controllers;

[ApiController]
[Route("config")]
public class ConfigController : ControllerBase
{
    private readonly IConfigurationService _configService;
    private readonly ILogger<ConfigController> _logger;

    public ConfigController(IConfigurationService configService, ILogger<ConfigController> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    /// <summary>
    /// Get a configuration value by hierarchical key.
    /// </summary>
    [HttpGet("{*key}")]
    [ProducesResponseType(typeof(ConfigResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(string key, CancellationToken ct)
    {
        try
        {
            var value = await _configService.GetAsync(key, ct);

            if (value is null)
            {
                return NotFound(new { error = $"Configuration key '{key}' not found" });
            }

            return Ok(new ConfigResponse { Key = key, Value = value });
        }
        catch (VaultConnectionException ex)
        {
            _logger.LogError(ex, "Failed to connect to vault");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Vault service unavailable" });
        }
        catch (VaultLockedException ex)
        {
            _logger.LogError(ex, "Vault is locked");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Check if a configuration key exists.
    /// </summary>
    [HttpHead("{*key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Exists(string key, CancellationToken ct)
    {
        try
        {
            var exists = await _configService.ExistsAsync(key, ct);
            return exists ? Ok() : NotFound();
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
        catch (VaultConnectionException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    /// <summary>
    /// List all configurations under a namespace.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ConfigListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> List([FromQuery] string prefix, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return BadRequest(new { error = "Query parameter 'prefix' is required" });
        }

        try
        {
            var configs = await _configService.ListAsync(prefix, ct);

            return Ok(new ConfigListResponse
            {
                Namespace = prefix,
                Configs = configs
            });
        }
        catch (VaultConnectionException ex)
        {
            _logger.LogError(ex, "Failed to connect to vault");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Vault service unavailable" });
        }
    }
}
