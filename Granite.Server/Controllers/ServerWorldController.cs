using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Granite.Common.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Granite.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/{serverid:guid}/world")]
public class ServerWorldController : ControllerBase
{
    private readonly ILogger<ServerWorldController> _logger;

    public ServerWorldController(ILogger<ServerWorldController> logger)
    {
        _logger = logger;
    }
    [HttpGet("collectibles")]
    public Task<ActionResult<List<CollectibleObjectDTO>>> GetCollectibles([FromRoute] Guid serverid)
    {
        throw new NotImplementedException("GetCollectibles endpoint not yet implemented");
    }

    [HttpPost("save")]
    public Task<ActionResult<string>> SaveWorld([FromRoute] Guid serverid)
    {
        throw new NotImplementedException("SaveWorld endpoint not yet implemented");
    }
}
