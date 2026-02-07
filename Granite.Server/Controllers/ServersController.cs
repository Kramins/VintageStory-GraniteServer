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
    private readonly ILogger<ServersController> _logger;
    private ServersService _serversService;

    public ServersController(ILogger<ServersController> logger, ServersService serversService)
    {
        _logger = logger;
        _serversService = serversService;
    }

    [HttpGet]
    public async Task<ActionResult<JsonApiDocument<List<ServerDTO>>>> GetServers()
    {
        var servers = await _serversService.GetServersAsync();
        return Ok(new JsonApiDocument<List<ServerDTO>>(servers));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<JsonApiDocument<ServerDTO>>> GetServerById(Guid id)
    {
        var server = await _serversService.GetServerByIdAsync(id);
        if (server == null)
        {
            return NotFound(
                new JsonApiDocument<ServerDTO>
                {
                    Errors =
                    {
                        new JsonApiError
                        {
                            Code = "NOT_FOUND",
                            Message = $"Server with ID {id} does not exist.",
                        },
                    },
                }
            );
        }

        return Ok(new JsonApiDocument<ServerDTO>(server));
    }

    [HttpPost]
    public async Task<ActionResult<JsonApiDocument<ServerCreatedResponseDTO>>> CreateServer(
        [FromBody] CreateServerRequestDTO request
    )
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(
                new JsonApiDocument<ServerCreatedResponseDTO>
                {
                    Errors =
                    {
                        new JsonApiError
                        {
                            Code = "INVALID_REQUEST",
                            Message = "Server name is required.",
                        },
                    },
                }
            );
        }

        try
        {
            var createdServer = await _serversService.CreateServerAsync(request);
            return CreatedAtAction(
                nameof(GetServerById),
                new { id = createdServer.Id },
                new JsonApiDocument<ServerCreatedResponseDTO>(createdServer)
            );
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create server: {Message}", ex.Message);
            return Conflict(
                new JsonApiDocument<ServerCreatedResponseDTO>
                {
                    Errors =
                    {
                        new JsonApiError { Code = "NAME_CONFLICT", Message = ex.Message },
                    },
                }
            );
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<JsonApiDocument<ServerDTO>>> UpdateServer(
        Guid id,
        [FromBody] UpdateServerRequestDTO request
    )
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(
                new JsonApiDocument<ServerDTO>
                {
                    Errors =
                    {
                        new JsonApiError
                        {
                            Code = "INVALID_REQUEST",
                            Message = "Server name is required.",
                        },
                    },
                }
            );
        }

        try
        {
            var updatedServer = await _serversService.UpdateServerAsync(id, request);
            if (updatedServer == null)
            {
                return NotFound(
                    new JsonApiDocument<ServerDTO>
                    {
                        Errors =
                        {
                            new JsonApiError
                            {
                                Code = "NOT_FOUND",
                                Message = $"Server with ID {id} does not exist.",
                            },
                        },
                    }
                );
            }

            return Ok(new JsonApiDocument<ServerDTO>(updatedServer));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to update server: {Message}", ex.Message);
            return Conflict(
                new JsonApiDocument<ServerDTO>
                {
                    Errors =
                    {
                        new JsonApiError { Code = "NAME_CONFLICT", Message = ex.Message },
                    },
                }
            );
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteServer(Guid id)
    {
        var deleted = await _serversService.DeleteServerAsync(id);
        if (!deleted)
        {
            return NotFound(
                new JsonApiDocument<object>
                {
                    Errors =
                    {
                        new JsonApiError
                        {
                            Code = "NOT_FOUND",
                            Message = $"Server with ID {id} does not exist.",
                        },
                    },
                }
            );
        }

        return NoContent();
    }

    [HttpPost("{id}/regenerate-token")]
    public async Task<
        ActionResult<JsonApiDocument<TokenRegeneratedResponseDTO>>
    > RegenerateAccessToken(Guid id)
    {
        var result = await _serversService.RegenerateAccessTokenAsync(id);
        if (result == null)
        {
            return NotFound(
                new JsonApiDocument<TokenRegeneratedResponseDTO>
                {
                    Errors =
                    {
                        new JsonApiError
                        {
                            Code = "NOT_FOUND",
                            Message = $"Server with ID {id} does not exist.",
                        },
                    },
                }
            );
        }

        return Ok(new JsonApiDocument<TokenRegeneratedResponseDTO>(result));
    }
}
