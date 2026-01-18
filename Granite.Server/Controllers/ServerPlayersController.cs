using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;
using Granite.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Granite.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/{serverId:guid}/players")]
public class ServerPlayersController : ControllerBase
{
    private ServerPlayersService _playerService;

    public ServerPlayersController(ServerPlayersService playerService)
    {
        _playerService = playerService;
    }

    [HttpGet]
    public Task<ActionResult<JsonApiDocument<IList<PlayerDTO>>>> GetAllPlayers(
        [FromRoute] Guid serverId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sorts = null,
        [FromQuery] string? filters = null
    )
    {
        throw new NotImplementedException("GetAllPlayers endpoint not yet implemented");
    }

    [HttpGet("{playerId}")]
    public Task<ActionResult<PlayerDetailsDTO>> GetPlayerById([FromRoute] Guid serverId, string playerId)
    {
        throw new NotImplementedException("GetPlayerById endpoint not yet implemented");
    }

    [HttpGet("find")]
    public Task<ActionResult<PlayerNameIdDTO>> FindPlayerByName(
        [FromRoute] Guid serverId,
        [FromQuery] string name
    )
    {
        throw new NotImplementedException("FindPlayerByName endpoint not yet implemented");
    }

    [HttpGet("sessions")]
    public Task<ActionResult<JsonApiDocument<IList<PlayerSessionDTO>>>> GetPlayerSessions(
        [FromRoute] Guid serverId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sorts = null,
        [FromQuery] string? filters = null
    )
    {
        throw new NotImplementedException("GetPlayerSessions endpoint not yet implemented");
    }

    [HttpPost("{playerId}/kick")]
    public Task<ActionResult<JsonApiDocument<string>>> KickPlayer(
        [FromRoute] Guid serverId,
        string playerId,
        [FromBody] KickRequestDTO request
    )
    {
        _playerService.KickPlayer(serverId, playerId, request.Reason);

        return Task.FromResult<ActionResult<JsonApiDocument<string>>>(
            new JsonApiDocument<string> { Data = "Player kicked successfully" }
        );
    }

    [HttpPost("{playerId}/ban")]
    public Task<ActionResult> BanPlayer(
        [FromRoute] Guid serverId,
        string playerId,
        [FromBody] BanRequestDTO request
    )
    {
        throw new NotImplementedException("BanPlayer endpoint not yet implemented");
    }

    [HttpDelete("{playerId}/ban")]
    public Task<ActionResult> UnbanPlayer([FromRoute] Guid serverId, string playerId)
    {
        throw new NotImplementedException("UnbanPlayer endpoint not yet implemented");
    }

    [HttpPost("{playerId}/whitelist")]
    public Task<ActionResult> WhitelistPlayer([FromRoute] Guid serverId, string playerId)
    {
        throw new NotImplementedException("WhitelistPlayer endpoint not yet implemented");
    }

    [HttpDelete("{playerId}/whitelist")]
    public Task<ActionResult> UnwhitelistPlayer([FromRoute] Guid serverId, string playerId)
    {
        throw new NotImplementedException("UnwhitelistPlayer endpoint not yet implemented");
    }

    [HttpPost("{playerId}/inventory/{slotIndex}")]
    public Task<ActionResult> UpdateInventorySlot(
        [FromRoute] Guid serverId,
        string playerId,
        int slotIndex,
        [FromBody] UpdateInventorySlotRequestDTO request
    )
    {
        throw new NotImplementedException("UpdateInventorySlot endpoint not yet implemented");
    }

    [HttpDelete("{playerId}/inventory/{slotIndex}")]
    public Task<ActionResult> RemoveInventorySlot(
        [FromRoute] Guid serverId,
        string playerId,
        int slotIndex
    )
    {
        throw new NotImplementedException("RemoveInventorySlot endpoint not yet implemented");
    }
}
