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
    private readonly ILogger<ServerPlayersController> _logger;
    private ServerPlayersService _playerService;
    private SieveProcessor _sieveProcessor;

    public ServerPlayersController(
        ILogger<ServerPlayersController> logger,
        ServerPlayersService playerService,
        SieveProcessor sieveProcessor
    )
    {
        _logger = logger;
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
        var playerDetails = await _playerService.GetPlayerDetailsAsync(serverId, playerId);

        if (playerDetails == null)
        {
            return NotFound();
        }

        return Ok(new JsonApiDocument<PlayerDetailsDTO> { Data = playerDetails });
    }

    [HttpGet("{playerId}/sessions")]
    public async Task<ActionResult<JsonApiDocument<IList<PlayerSessionDTO>>>> GetPlayerSessions(
        [FromRoute] Guid serverId,
        [FromRoute] string playerId,
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

        var sessions = await _playerService.GetPlayerSessionsAsync(serverId, playerId, sieveModel);

        return new JsonApiDocument<IList<PlayerSessionDTO>>
        {
            Data = sessions.Data,
            Meta = new JsonApiMeta
            {
                Pagination = new PaginationMeta
                {
                    Page = page,
                    PageSize = pageSize,
                    HasMore = sessions.Data?.Count >= pageSize,
                    TotalCount = sessions.TotalCount,
                },
            },
        };
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{playerId}/kick")]
    public async Task<ActionResult<JsonApiDocument<string>>> KickPlayer(
        [FromRoute] Guid serverId,
        string playerId,
        [FromBody] KickRequestDTO request
    )
    {
        await _playerService.KickPlayer(serverId, playerId, request.Reason);

        return new JsonApiDocument<string> { Data = "Player kicked successfully" };
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{playerId}/ban")]
    public async Task<ActionResult<JsonApiDocument<string>>> BanPlayer(
        [FromRoute] Guid serverId,
        string playerId,
        [FromBody] BanRequestDTO request
    )
    {
        await _playerService.BanPlayer(
            serverId,
            playerId,
            request.Reason ?? string.Empty,
            request.UntilDate,
            request.IssuedBy
        );

        return new JsonApiDocument<string> { Data = "Player banned successfully" };
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{playerId}/ban")]
    public async Task<ActionResult<JsonApiDocument<string>>> UnbanPlayer(
        [FromRoute] Guid serverId,
        string playerId
    )
    {
        await _playerService.UnbanPlayer(serverId, playerId);

        return new JsonApiDocument<string> { Data = "Player unbanned successfully" };
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{playerId}/whitelist")]
    public async Task<ActionResult<JsonApiDocument<string>>> WhitelistPlayer(
        [FromRoute] Guid serverId,
        string playerId,
        [FromBody] WhitelistRequestDTO? request = null
    )
    {
        await _playerService.WhitelistPlayer(serverId, playerId, request?.Reason);

        return new JsonApiDocument<string> { Data = "Player whitelisted successfully" };
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{playerId}/whitelist")]
    public async Task<ActionResult<JsonApiDocument<string>>> UnwhitelistPlayer(
        [FromRoute] Guid serverId,
        string playerId
    )
    {
        await _playerService.UnwhitelistPlayer(serverId, playerId);

        return new JsonApiDocument<string> { Data = "Player unwhitelisted successfully" };
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{playerId}/inventory/{inventoryName}/{slotIndex}")]
    public async Task<ActionResult<JsonApiDocument<string>>> UpdateInventorySlot(
        [FromRoute] Guid serverId,
        string playerId,
        string inventoryName,
        int slotIndex,
        [FromBody] UpdateInventorySlotRequestDTO request
    )
    {
        await _playerService.UpdateInventorySlot(
            serverId,
            playerId,
            inventoryName,
            slotIndex,
            request
        );

        return new JsonApiDocument<string> { Data = "Inventory slot updated successfully" };
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{playerId}/inventory/{inventoryName}/{slotIndex}")]
    public async Task<ActionResult<JsonApiDocument<string>>> RemoveInventorySlot(
        [FromRoute] Guid serverId,
        string playerId,
        string inventoryName,
        int slotIndex
    )
    {
        await _playerService.RemoveInventorySlot(serverId, playerId, inventoryName, slotIndex);

        return new JsonApiDocument<string> { Data = "Inventory slot removed successfully" };
    }
}
