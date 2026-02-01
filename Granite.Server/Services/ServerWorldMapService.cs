using System;
using System.Linq;
using System.Threading.Tasks;
using Granite.Common.Dto;
using Granite.Server.Services.Map;
using GraniteServer.Data;
using Microsoft.EntityFrameworkCore;

namespace Granite.Server.Services;

public class ServerWorldMapService
{
    private readonly IMapDataStorageService _mapDataStorage;
    private readonly IMapRenderingService _mapRendering;
    private readonly GraniteDataContext _dbContext;
    private readonly ILogger<ServerWorldMapService> _logger;

    public ServerWorldMapService(
        IMapDataStorageService mapDataStorage,
        IMapRenderingService mapRendering,
        GraniteDataContext dbContext,
        ILogger<ServerWorldMapService> logger)
    {
        _mapDataStorage = mapDataStorage;
        _mapRendering = mapRendering;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<WorldMapBoundsDTO?> GetWorldBoundsAsync(Guid serverId)
    {
        try
        {
            var chunks = await _dbContext.MapChunks
                .Where(c => c.ServerId == serverId)
                .Select(c => new { c.ChunkX, c.ChunkZ })
                .ToListAsync();

            if (!chunks.Any())
            {
                _logger.LogWarning("No map chunks found for server {ServerId}", serverId);
                return null;
            }

            //// TODO: Consider doing the min and max calculations in the database query for efficiency
            return new WorldMapBoundsDTO
            {
                MinChunkX = chunks.Min(c => c.ChunkX),
                MaxChunkX = chunks.Max(c => c.ChunkX),
                MinChunkZ = chunks.Min(c => c.ChunkZ),
                MaxChunkZ = chunks.Max(c => c.ChunkZ),
                TotalChunks = chunks.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving world bounds for server {ServerId}", serverId);
            throw;
        }
    }

    public async Task<byte[]?> GetTileImageAsync(Guid serverId, int chunkX, int chunkZ)
    {
        try
        {
            var chunk = await _dbContext.MapChunks
                .FirstOrDefaultAsync(c => c.ServerId == serverId && c.ChunkX == chunkX && c.ChunkZ == chunkZ);

            if (chunk == null)
            {
                _logger.LogWarning("Chunk ({ChunkX}, {ChunkZ}) not found for server {ServerId}", chunkX, chunkZ, serverId);
                return null;
            }

            // Use the rendering service which includes caching
            var imageBytes = await _mapRendering.RenderChunkTileAsync(serverId, chunkX, chunkZ);

            return imageBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering tile image for chunk ({ChunkX}, {ChunkZ}) on server {ServerId}", chunkX, chunkZ, serverId);
            throw;
        }
    }

    public async Task<MapTileMetadataDTO?> GetTileMetadataAsync(Guid serverId, int chunkX, int chunkZ)
    {
        try
        {
            var chunk = await _dbContext.MapChunks
                .FirstOrDefaultAsync(c => c.ServerId == serverId && c.ChunkX == chunkX && c.ChunkZ == chunkZ);

            if (chunk == null)
            {
                _logger.LogWarning("Chunk ({ChunkX}, {ChunkZ}) not found for server {ServerId}", chunkX, chunkZ, serverId);
                return null;
            }

            // Chunk size is fixed at 32x32 (from MapRenderingService.ChunkSize)
            const int chunkSize = 32;

            return new MapTileMetadataDTO
            {
                ChunkX = chunk.ChunkX,
                ChunkZ = chunk.ChunkZ,
                ChunkHash = chunk.ContentHash,
                Width = chunkSize,
                Height = chunkSize,
                ExtractedAt = chunk.ExtractedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tile metadata for chunk ({ChunkX}, {ChunkZ}) on server {ServerId}", chunkX, chunkZ, serverId);
            throw;
        }
    }
}
