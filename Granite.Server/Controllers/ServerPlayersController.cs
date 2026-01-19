using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;
using Granite.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sieve.Models;
using Sieve.Services;

namespace Granite.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/{serverId:guid}/players")]
public class ServerPlayersController : ControllerBase
{
    private ServerPlayersService _playerService;
    private SieveProcessor _sieveProcessor;

    public ServerPlayersController(
        ServerPlayersService playerService,
        SieveProcessor sieveProcessor
    )
    {
        _playerService = playerService;
        _sieveProcessor = sieveProcessor;
    }

    [HttpGet]
    public async Task<ActionResult<JsonApiDocument<IList<PlayerDTO>>>> GetAllPlayers(
        [FromRoute] Guid serverId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sorts = null,
        [FromQuery] string? filters = null
    )
    {
        var sieveModel = new SieveModel
        {
            Filters = filters,
            Sorts = sorts,
            Page = page,
            PageSize = pageSize,
        };

        var allPlayers = await _playerService.GetPlayersAsync(serverId);
        var query = allPlayers.AsQueryable();

        var totalCount = query.Count();
        query = _sieveProcessor.Apply(sieveModel, query);
        var pagedPlayers = query.ToList();

        return new JsonApiDocument<IList<PlayerDTO>>
        {
            Data = pagedPlayers,
            Meta = new JsonApiMeta
            {
                Pagination = new PaginationMeta
                {
                    Page = page,
                    PageSize = pageSize,
                    HasMore = pagedPlayers.Count >= pageSize,
                    TotalCount = totalCount,
                },
            },
        };
    }

    [HttpGet("{playerId}")]
    public async Task<ActionResult<JsonApiDocument<PlayerDetailsDTO>>> GetPlayerById(
        [FromRoute] Guid serverId,
        string playerId
    )
    {
        if (!Guid.TryParse(playerId, out var playerGuid))
        {
            return BadRequest("Invalid playerId format");
        }

        var playerDetails = await _playerService.GetPlayerDetailsAsync(serverId, playerGuid);

        if (playerDetails == null)
        {
            return NotFound();
        }

        return Ok(new JsonApiDocument<PlayerDetailsDTO> { Data = playerDetails });
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
    public Task<ActionResult<JsonApiDocument<string>>> BanPlayer(
        [FromRoute] Guid serverId,
        string playerId,
        [FromBody] BanRequestDTO request
    )
    {
        _playerService.BanPlayer(
            serverId,
            playerId,
            request.Reason ?? string.Empty,
            request.UntilDate,
            request.IssuedBy
        );

        return Task.FromResult<ActionResult<JsonApiDocument<string>>>(
            new JsonApiDocument<string> { Data = "Player banned successfully" }
        );
    }

    [HttpDelete("{playerId}/ban")]
    public Task<ActionResult<JsonApiDocument<string>>> UnbanPlayer(
        [FromRoute] Guid serverId,
        string playerId
    )
    {
        _playerService.UnbanPlayer(serverId, playerId);

        return Task.FromResult<ActionResult<JsonApiDocument<string>>>(
            new JsonApiDocument<string> { Data = "Player unbanned successfully" }
        );
    }

    [HttpPost("{playerId}/whitelist")]
    public Task<ActionResult<JsonApiDocument<string>>> WhitelistPlayer(
        [FromRoute] Guid serverId,
        string playerId,
        [FromBody] WhitelistRequestDTO? request = null
    )
    {
        _playerService.WhitelistPlayer(serverId, playerId, request?.Reason);

        return Task.FromResult<ActionResult<JsonApiDocument<string>>>(
            new JsonApiDocument<string> { Data = "Player whitelisted successfully" }
        );
    }

    [HttpDelete("{playerId}/whitelist")]
    public Task<ActionResult<JsonApiDocument<string>>> UnwhitelistPlayer(
        [FromRoute] Guid serverId,
        string playerId
    )
    {
        _playerService.UnwhitelistPlayer(serverId, playerId);

        return Task.FromResult<ActionResult<JsonApiDocument<string>>>(
            new JsonApiDocument<string> { Data = "Player unwhitelisted successfully" }
        );
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
