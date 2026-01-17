using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Granite.Common.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Granite.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WorldController : ControllerBase
{
    [HttpGet("collectibles")]
    public Task<ActionResult<List<CollectibleObjectDTO>>> GetCollectibles()
    {
        throw new NotImplementedException("GetCollectibles endpoint not yet implemented");
    }

    [HttpPost("save")]
    public Task<ActionResult<string>> SaveWorld()
    {
        throw new NotImplementedException("SaveWorld endpoint not yet implemented");
    }
}
