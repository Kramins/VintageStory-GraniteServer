using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;
using GraniteServer.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Granite.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/{serverId:guid}/collectibles")]
public class ServerCollectiblesController : ControllerBase
{
    private readonly ILogger<ServerCollectiblesController> _logger;
    private readonly GraniteDataContext _dbContext;

    public ServerCollectiblesController(
        ILogger<ServerCollectiblesController> logger,
        GraniteDataContext dbContext
    )
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Get all collectibles (items and blocks) available on the server.
    /// This includes all registered collectible objects from the game and mods.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<JsonApiDocument<IList<CollectibleObjectDTO>>>> GetAllCollectibles(
        [FromRoute] Guid serverId,
        [FromQuery] string? type = null
    )
    {
        var query = _dbContext.Collectibles.Where(c => c.ServerId == serverId);

        if (!string.IsNullOrEmpty(type))
        {
            query = query.Where(c => c.Type == type);
        }

        var collectibles = await query
            .OrderBy(c => c.Name)
            .Select(c => new CollectibleObjectDTO
            {
                Id = c.CollectibleId,
                Name = c.Name,
                Type = c.Type,
                MaxStackSize = c.MaxStackSize,
                Class = c.Class,
            })
            .ToListAsync();

        return new JsonApiDocument<IList<CollectibleObjectDTO>> { Data = collectibles };
    }

    /// <summary>
    /// Get a specific collectible by ID.
    /// </summary>
    [HttpGet("{collectibleId:int}")]
    public async Task<ActionResult<JsonApiDocument<CollectibleObjectDTO>>> GetCollectibleById(
        [FromRoute] Guid serverId,
        [FromRoute] int collectibleId
    )
    {
        var collectible = await _dbContext
            .Collectibles.Where(c => c.ServerId == serverId && c.CollectibleId == collectibleId)
            .Select(c => new CollectibleObjectDTO
            {
                Id = c.CollectibleId,
                Name = c.Name,
                Type = c.Type,
                MaxStackSize = c.MaxStackSize,
                Class = c.Class,
            })
            .FirstOrDefaultAsync();

        if (collectible == null)
        {
            return NotFound(
                new JsonApiDocument<string>
                {
                    Errors = new List<JsonApiError>
                    {
                        new JsonApiError
                        {
                            Code = "404",
                            Message = $"Collectible with ID {collectibleId} not found on server {serverId}",
                        },
                    },
                }
            );
        }

        return new JsonApiDocument<CollectibleObjectDTO> { Data = collectible };
    }
}
