using System;
using System.Threading.Tasks;
using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;
using Granite.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Granite.Server.Controllers
{
    [Authorize]
    [Route("api/worldmap")]
    [ApiController]
    public class ServerWorldMapController : ControllerBase
    {
        private readonly IServerWorldMapService _worldMapService;
        private readonly ILogger<ServerWorldMapController> _logger;

        public ServerWorldMapController(
            IServerWorldMapService worldMapService,
            ILogger<ServerWorldMapController> logger
        )
        {
            _worldMapService = worldMapService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the bounds of all map chunks for a server.
        /// </summary>
        [HttpGet("{serverid:guid}/bounds")]
        public async Task<ActionResult<JsonApiDocument<WorldMapBoundsDTO>>> GetWorldBounds(
            [FromRoute] Guid serverid)
        {
            try
            {
                var bounds = await _worldMapService.GetWorldBoundsAsync(serverid);

                if (bounds == null)
                {
                    return NotFound(new JsonApiDocument<WorldMapBoundsDTO>
                        {
                            Errors = new List<JsonApiError>
                            {
                                new JsonApiError
                                {
                                    Code = "404",
                                Message = $"No map data found for server {serverid}"
                            }
                        }
                    });
                }

                return Ok(new JsonApiDocument<WorldMapBoundsDTO> { Data = bounds });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving world bounds for server {ServerId}", serverid);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new JsonApiDocument<WorldMapBoundsDTO>
                    {
                        Errors = new List<JsonApiError>
                        {
                            new JsonApiError
                            {
                                Code = "500",
                                Message = "An error occurred while retrieving world bounds"
                    }
                        }
                    });
            }
        }

        /// <summary>
        /// Gets the rendered PNG image for a specific map chunk.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{serverid:guid}/tiles/{chunkX:int}/{chunkZ:int}")]
        public async Task<IActionResult> GetTileImage(
            [FromRoute] Guid serverid,
            [FromRoute] int chunkX,
            [FromRoute] int chunkZ
        )
        {
            try
            {
                var imageBytes = await _worldMapService.GetTileImageAsync(serverid, chunkX, chunkZ);

                if (imageBytes == null)
                {
                    return NotFound(
                        new JsonApiDocument<object>
                        {
                            Errors = new List<JsonApiError>
                            {
                                new JsonApiError
                                {
                                    Code = "404",
                                    Message =
                                        $"Tile  chunk={chunkX},{chunkZ}) not found for server {serverid}",
                                },
                            },
                        }
                    );
                }

                // Get metadata for ETag generation
                var metadata = await _worldMapService.GetTileMetadataAsync(
                    serverid,
                    chunkX,
                    chunkZ
                );
                if (metadata != null)
                {
                    // Use chunk hash as ETag for cache validation
                    Response.Headers.Append("ETag", $"\"{metadata.ChunkHash}\"");
                    Response.Headers.Append("Cache-Control", "public, max-age=3600");
                }

                return File(imageBytes, "image/png");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving tile image for (chunk {ChunkX}, {ChunkZ}) on server {ServerId}",
                    chunkX,
                    chunkZ,
                    serverid
                );
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new JsonApiDocument<object>
                    {
                        Errors = new List<JsonApiError>
                        {
                            new JsonApiError
                            {
                                Code = "500",
                                Message = "An error occurred while retrieving the tile image",
                            },
                        },
                    }
                );
            }
        }

        /// <summary>
        /// Gets a grouped tile image (256×256 pixels) from 8×8 chunks with fog of war for missing chunks.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{serverid:guid}/tiles/grouped/{groupX:int}/{groupZ:int}")]
        public async Task<IActionResult> GetGroupedTileImage(
            [FromRoute] Guid serverid,
            [FromRoute] int groupX,
            [FromRoute] int groupZ
        )
        {
            try
            {
                var imageBytes = await _worldMapService.GetGroupedTileImageAsync(
                    serverid,
                    groupX,
                    groupZ
                );

                if (imageBytes == null)
                {
                    return NotFound(
                        new JsonApiDocument<object>
                        {
                            Errors = new List<JsonApiError>
                            {
                                new JsonApiError
                                {
                                    Code = "404",
                                    Message =
                                        $"Grouped tile (group={groupX},{groupZ}) not found for server {serverid}",
                                },
                            },
                        }
                    );
                }

                Response.Headers.Append("Cache-Control", "public, max-age=86400"); // 24 hours for grouped tiles
                return File(imageBytes, "image/png");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving grouped tile image for (group {GroupX}, {GroupZ}) on server {ServerId}",
                    groupX,
                    groupZ,
                    serverid
                );
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new JsonApiDocument<object>
                    {
                        Errors = new List<JsonApiError>
                        {
                            new JsonApiError
                            {
                                Code = "500",
                                Message =
                                    "An error occurred while retrieving the grouped tile image",
                            },
                        },
                    }
                );
            }
        }

        /// <summary>
        /// Gets metadata for a specific map chunk (hash, dimensions, timestamp).
        /// Accepts OpenLayers tile coordinates where Y is inverted from game Z coordinate.
        /// </summary>
        [HttpGet("{serverid:guid}/tiles/{x:int}/{y:int}/metadata")]
        public async Task<ActionResult<JsonApiDocument<MapTileMetadataDTO>>> GetTileMetadata(
            [FromRoute] Guid serverid,
            [FromRoute] int x,
            [FromRoute] int y
        )
        {
            // Convert OpenLayers coordinates to game coordinates
            var chunkX = x;
            var chunkZ = -y;

            try
            {
                var metadata = await _worldMapService.GetTileMetadataAsync(
                    serverid,
                    chunkX,
                    chunkZ
                );

                if (metadata == null)
                {
                    return NotFound(
                        new JsonApiDocument<MapTileMetadataDTO>
                        {
                            Errors = new List<JsonApiError>
                            {
                                new JsonApiError
                                {
                                    Code = "404",
                                    Message =
                                        $"Tile (x={x}, y={y}, chunk={chunkX},{chunkZ}) not found for server {serverid}",
                                },
                            },
                        }
                    );
                }

                return Ok(new JsonApiDocument<MapTileMetadataDTO> { Data = metadata });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving tile metadata for x={X}, y={Y} (chunk {ChunkX}, {ChunkZ}) on server {ServerId}",
                    x,
                    y,
                    chunkX,
                    chunkZ,
                    serverid
                );
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new JsonApiDocument<MapTileMetadataDTO>
                    {
                        Errors = new List<JsonApiError>
                        {
                            new JsonApiError
                            {
                                Code = "500",
                                Message = "An error occurred while retrieving tile metadata",
                            },
                        },
                    }
                );
            }
        }
    }
}
