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
    private readonly ServersService _serversService;

    public ServerController(ILogger<ServerController> logger, ServersService serversService)
    {
        _logger = logger;
        _serversService = serversService;
    }

    [HttpGet("status")]
    public async Task<ActionResult<JsonApiDocument<ServerDetailsDTO>>> GetServerStatus(
        [FromRoute] Guid serverid
    )
    {
        var status = await _serversService.GetServerDetailsAsync(serverid);

        if (status == null)
        {
            return NotFound(
                new JsonApiDocument<ServerDetailsDTO>
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

        return new JsonApiDocument<ServerDetailsDTO> { Data = status };
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

        await _serversService.AnnounceMessageAsync(serverid, request.Message);

        return new JsonApiDocument<string> { Data = "Message announced successfully" };
    }
}
