using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;
using Granite.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Granite.Server.Controllers;

[Authorize]
[Route("api/servers")]
[ApiController]
public class ServersController : ControllerBase
{
    private ServersService _serversService;

    public ServersController(ServersService serversService)
    {
        _serversService = serversService;
    }
    [HttpGet]
    public async Task<ActionResult<JsonApiDocument<List<ServerDTO>>>> GetServers()
    {
        var servers = await _serversService.GetServersAsync();
        return Ok(new JsonApiDocument<List<ServerDTO>>(servers));
    }
}
