using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Granite.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PlayerController : ControllerBase
{
    [HttpGet]
    public Task<ActionResult<JsonApiDocument<IList<PlayerDTO>>>> GetAllPlayers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sorts = null,
        [FromQuery] string? filters = null
    )
    {
        throw new NotImplementedException("GetAllPlayers endpoint not yet implemented");
    }

    [HttpGet("{id}")]
    public Task<ActionResult<PlayerDetailsDTO>> GetPlayerById(string id)
    {
        throw new NotImplementedException("GetPlayerById endpoint not yet implemented");
    }

    [HttpGet("find")]
    public Task<ActionResult<PlayerNameIdDTO>> FindPlayerByName([FromQuery] string name)
    {
        throw new NotImplementedException("FindPlayerByName endpoint not yet implemented");
    }

    [HttpGet("sessions")]
    public Task<ActionResult<JsonApiDocument<IList<PlayerSessionDTO>>>> GetPlayerSessions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sorts = null,
        [FromQuery] string? filters = null
    )
    {
        throw new NotImplementedException("GetPlayerSessions endpoint not yet implemented");
    }

    [HttpPost("{id}/kick")]
    public Task<ActionResult<string>> KickPlayer(string id, [FromBody] KickRequestDTO request)
    {
        throw new NotImplementedException("KickPlayer endpoint not yet implemented");
    }

    [HttpPost("{id}/ban")]
    public Task<ActionResult> BanPlayer(string id, [FromBody] BanRequestDTO request)
    {
        throw new NotImplementedException("BanPlayer endpoint not yet implemented");
    }

    [HttpDelete("{id}/ban")]
    public Task<ActionResult> UnbanPlayer(string id)
    {
        throw new NotImplementedException("UnbanPlayer endpoint not yet implemented");
    }

    [HttpPost("{id}/whitelist")]
    public Task<ActionResult> WhitelistPlayer(string id)
    {
        throw new NotImplementedException("WhitelistPlayer endpoint not yet implemented");
    }

    [HttpDelete("{id}/whitelist")]
    public Task<ActionResult> UnwhitelistPlayer(string id)
    {
        throw new NotImplementedException("UnwhitelistPlayer endpoint not yet implemented");
    }

    [HttpPost("{id}/inventory/{slotIndex}")]
    public Task<ActionResult> UpdateInventorySlot(
        string id,
        int slotIndex,
        [FromBody] UpdateInventorySlotRequestDTO request
    )
    {
        throw new NotImplementedException("UpdateInventorySlot endpoint not yet implemented");
    }

    [HttpDelete("{id}/inventory/{slotIndex}")]
    public Task<ActionResult> RemoveInventorySlot(string id, int slotIndex)
    {
        throw new NotImplementedException("RemoveInventorySlot endpoint not yet implemented");
    }
}
