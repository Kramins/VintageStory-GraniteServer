using System;
using System.Threading.Tasks;
using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;
using Granite.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Granite.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/{serverid:guid}/server")]
public class ServerController : ControllerBase
{
    private readonly ILogger<ServerController> _logger;
    private readonly ServerService _serverService;

    public ServerController(ILogger<ServerController> logger, ServerService serverService)
    {
        _logger = logger;
        _serverService = serverService;
    }

    [HttpGet("status")]
    public async Task<ActionResult<JsonApiDocument<ServerStatusDTO>>> GetServerStatus(
        [FromRoute] Guid serverid
    )
    {
        var status = await _serverService.GetServerStatusAsync(serverid);

        if (status == null)
        {
            return NotFound(
                new JsonApiDocument<ServerStatusDTO>
                {
                    Errors = new List<JsonApiError>
                    {
                        new JsonApiError
                        {
                            Code = "404",
                            Message = $"Server with ID {serverid} not found",
                        },
                    },
                }
            );
        }

        return new JsonApiDocument<ServerStatusDTO> { Data = status };
    }

    [HttpPost("announce")]
    public async Task<ActionResult<JsonApiDocument<string>>> AnnounceMessage(
        [FromRoute] Guid serverid,
        [FromBody] AnnounceMessageDTO request
    )
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(
                new JsonApiDocument<string>
                {
                    Errors = new List<JsonApiError>
                    {
                        new JsonApiError { Code = "400", Message = "Message cannot be empty", },
                    },
                }
            );
        }

        await _serverService.AnnounceMessageAsync(serverid, request.Message);

        return new JsonApiDocument<string> { Data = "Message announced successfully" };
    }
}
