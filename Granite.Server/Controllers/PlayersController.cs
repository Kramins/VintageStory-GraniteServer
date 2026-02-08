using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;
using Granite.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Granite.Server.Controllers
{
    [Route("api/players")]
    [ApiController]
    public class PlayersController : ControllerBase
    {
        private readonly IPlayersService _playersService;

        public PlayersController(IPlayersService playersService)
        {
            _playersService = playersService;
        }

        /// <summary>
        /// Finds a player by name across all servers. Searches the database first,
        /// then falls back to the Vintage Story authentication server if not found.
        /// Results are cached to reduce external API calls.
        /// Rate limited to 10 requests per minute per IP to protect the auth server.
        /// </summary>
        /// <param name="name">The player name to search for</param>
        /// <returns>Player name and ID if found</returns>
        [HttpGet("find")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("PlayerSearchLimit")]
        public async Task<ActionResult<JsonApiDocument<PlayerNameIdDTO>>> FindPlayerByName(
            [FromQuery] string name
        )
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(
                    new JsonApiDocument<PlayerNameIdDTO>
                    {
                        Errors =
                        {
                            new JsonApiError
                            {
                                Code = "INVALID_REQUEST",
                                Message = "Player name is required",
                            },
                        },
                    }
                );
            }

            try
            {
                var player = await _playersService.FindPlayerByNameAsync(
                    name,
                    HttpContext.RequestAborted
                );

                if (player == null)
                {
                    return NotFound(
                        new JsonApiDocument<PlayerNameIdDTO>
                        {
                            Errors =
                            {
                                new JsonApiError
                                {
                                    Code = "NOT_FOUND",
                                    Message = $"Player '{name}' not found",
                                },
                            },
                        }
                    );
                }

                return Ok(new JsonApiDocument<PlayerNameIdDTO>(player));
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new JsonApiDocument<PlayerNameIdDTO>
                    {
                        Errors =
                        {
                            new JsonApiError
                            {
                                Code = "SERVER_ERROR",
                                Message = "Failed to resolve player name",
                                StackTrace = ex.Message,
                            },
                        },
                    }
                );
            }
        }
    }
}
