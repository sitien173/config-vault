using ConfigVault.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConfigVault.Api.Controllers;

[ApiController]
[Route("events")]
public class EventsController : ControllerBase
{
    private readonly SseConnectionManager _connectionManager;
    private readonly ILogger<EventsController> _logger;

    public EventsController(SseConnectionManager connectionManager, ILogger<EventsController> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
    }

    /// <summary>
    /// Subscribe to configuration change events via Server-Sent Events.
    /// </summary>
    /// <param name="filter">Optional glob pattern to filter keys (e.g., "production/*", "*/database/**")</param>
    [HttpGet]
    [Produces("text/event-stream")]
    public async Task Get([FromQuery] string? filter, CancellationToken ct)
    {
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.ContentType = "text/event-stream";

        var client = _connectionManager.RegisterClient(filter);

        try
        {
            await foreach (var sseEvent in client.EventChannel.Reader.ReadAllAsync(ct))
            {
                await Response.WriteAsync($"event: {sseEvent.EventType}\n", ct);
                await Response.WriteAsync($"data: {sseEvent.Data}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("SSE client disconnected");
        }
        finally
        {
            _connectionManager.UnregisterClient(client.Id);
        }
    }
}
