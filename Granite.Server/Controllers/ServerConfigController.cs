using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;
using Granite.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Granite.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/{serverId:guid}/config")]
public class ServerConfigController : ControllerBase
{
    private readonly ILogger<ServerConfigController> _logger;
    private readonly ServerConfigService _configService;

    public ServerConfigController(ILogger<ServerConfigController> logger, ServerConfigService configService)
    {
        _logger = logger;
        _configService = configService;
    }

    /// <summary>
    /// Get the current server configuration.
    /// Note: This returns the last synced configuration from the database.
    /// Use the sync endpoint to request a fresh configuration from the game server.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<JsonApiDocument<ServerConfigDTO>>> GetServerConfig(
        [FromRoute] Guid serverId
    )
    {
        var config = await _configService.GetServerConfigAsync(serverId);

        if (config == null)
        {
            return NotFound(
                new JsonApiDocument<ServerConfigDTO>
                {
                    Errors = new List<JsonApiError>
                    {
                        new JsonApiError
                        {
                            Code = "404",
                            Message = $"Server with ID {serverId} not found",
                        },
                    },
                }
            );
        }

        return new JsonApiDocument<ServerConfigDTO> { Data = config };
    }

    /// <summary>
    /// Request the game server to sync its current configuration to the control plane.
    /// This will trigger a ServerConfigSyncedEvent from the mod.
    /// </summary>
    [HttpPost("sync")]
    public async Task<ActionResult<JsonApiDocument<object>>> SyncServerConfig(
        [FromRoute] Guid serverId
    )
    {
        await _configService.SyncServerConfigAsync(serverId);

        return new JsonApiDocument<object>
        {
            Data = new { Message = "Configuration sync command sent to game server" },
        };
    }

    /// <summary>
    /// Update the server configuration.
    /// This will send an UpdateServerConfigCommand to the game server mod.
    /// Only non-null properties in the request body will be updated.
    /// </summary>
    [HttpPatch]
    public async Task<ActionResult<JsonApiDocument<object>>> UpdateServerConfig(
        [FromRoute] Guid serverId,
        [FromBody] ServerConfigDTO config
    )
    {
        await _configService.UpdateServerConfigAsync(serverId, config);

        return new JsonApiDocument<object>
        {
            Data = new
            {
                Message =
                    "Configuration update command sent to game server. Changes will be applied shortly.",
            },
        };
    }
}
