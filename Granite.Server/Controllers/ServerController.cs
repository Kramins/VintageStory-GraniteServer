using System;
using System.Threading.Tasks;
using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Granite.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/{serverid:guid}/server")]
public class ServerController : ControllerBase
{
    [HttpGet("status")]
    public Task<ActionResult<ServerStatusDTO>> GetServerStatus([FromRoute] Guid serverid)
    {
        throw new NotImplementedException("GetServerStatus endpoint not yet implemented");
    }

    [HttpPost("announce")]
    public Task<ActionResult<string>> AnnounceMessage(
        [FromRoute] Guid serverid,
        [FromBody] AnnounceMessageDTO request
    )
    {
        throw new NotImplementedException("AnnounceMessage endpoint not yet implemented");
    }

    [HttpGet("config")]
    public Task<ActionResult<JsonApiDocument<ServerConfigDTO>>> GetServerConfig([FromRoute] Guid serverid)
    {
        throw new NotImplementedException("GetServerConfig endpoint not yet implemented");
    }

    [HttpPost("config")]
    public Task<ActionResult<JsonApiDocument<string>>> UpdateServerConfig(
        [FromRoute] Guid serverid,
        [FromBody] ServerConfigDTO config
    )
    {
        throw new NotImplementedException("UpdateServerConfig endpoint not yet implemented");
    }
}
