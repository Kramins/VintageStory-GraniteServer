using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Granite.Common.Dto;
using Granite.Server.Models;
using Granite.Server.Services.Map;
using GraniteServer.Data;
using GraniteServer.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Granite.Server.Services;

public class ServerWorldMapService : IServerWorldMapService
{
    private readonly IMapRenderingService _mapRendering;
    private readonly GraniteDataContext _dbContext;
    private readonly ILogger<ServerWorldMapService> _logger;

    public ServerWorldMapService(
        IMapRenderingService mapRendering,
        GraniteDataContext dbContext,
        ILogger<ServerWorldMapService> logger
    )
    {
        _mapRendering = mapRendering;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> StoreChunkDataAsync(
        Guid serverId,
        int chunkX,
        int chunkZ,
        string contentHash,
        int[] rainHeightMap,
        int[] surfaceBlockIds,
        DateTime extractedAt
    )
    {
        var existing = await _dbContext.MapChunks.FirstOrDefaultAsync(c =>
            c.ServerId == serverId && c.ChunkX == chunkX && c.ChunkZ == chunkZ
        );

        if (existing != null)
        {
            // Check if data has changed
            if (existing.ContentHash == contentHash)
            {
                _logger.LogDebug(
                    "Chunk ({ChunkX}, {ChunkZ}) for server {ServerId} unchanged (hash: {Hash})",
                    chunkX,
                    chunkZ,
                    serverId,
                    contentHash[..8]
                );
                return false;
            }

            // Update existing
            existing.ContentHash = contentHash;
            existing.RainHeightMapData = rainHeightMap;
            existing.SurfaceBlockIdsData = surfaceBlockIds;
            existing.ExtractedAt = extractedAt;
            existing.ReceivedAt = DateTime.UtcNow;
            existing.LastAccessedAt = DateTime.UtcNow;

            _logger.LogDebug(
                "Updated chunk ({ChunkX}, {ChunkZ}) for server {ServerId} (new hash: {Hash})",
                chunkX,
                chunkZ,
                serverId,
                contentHash[..8]
            );
        }
        else
        {
            // Insert new
            var entity = new MapChunkEntity
            {
                ServerId = serverId,
                ChunkX = chunkX,
                ChunkZ = chunkZ,
                ContentHash = contentHash,
                RainHeightMapData = rainHeightMap,
                SurfaceBlockIdsData = surfaceBlockIds,
                ExtractedAt = extractedAt,
                ReceivedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow,
            };

            _dbContext.MapChunks.Add(entity);

            _logger.LogDebug(
                "Stored new chunk ({ChunkX}, {ChunkZ}) for server {ServerId} (hash: {Hash})",
                chunkX,
                chunkZ,
                serverId,
                contentHash[..8]
            );
        }

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<List<StoredChunkHashDTO>> GetAllChunkHashesAsync(Guid serverId)
    {
        var hashes = await _dbContext
            .MapChunks.Where(c => c.ServerId == serverId)
            .Select(c => new StoredChunkHashDTO(c.ChunkX, c.ChunkZ, c.ContentHash))
            .ToListAsync();

        return hashes;
    }

    public async Task<StoredChunkData?> GetChunkDataAsync(Guid serverId, int chunkX, int chunkZ)
    {
        var entity = await _dbContext.MapChunks.FirstOrDefaultAsync(c =>
            c.ServerId == serverId && c.ChunkX == chunkX && c.ChunkZ == chunkZ
        );

        if (entity == null)
            return null;

        // Update last accessed time, I don't think we need todo this
        // entity.LastAccessedAt = DateTime.UtcNow;
        // await _dbContext.SaveChangesAsync();

        return new StoredChunkData
        {
            ChunkX = entity.ChunkX,
            ChunkZ = entity.ChunkZ,
            ContentHash = entity.ContentHash,
            RainHeightMap = entity.RainHeightMapData,
            SurfaceBlockId = entity.SurfaceBlockIdsData,
        };
    }

    public async Task<WorldMapBoundsDTO?> GetWorldBoundsAsync(Guid serverId)
    {
        try
        {
            var chunks = await _dbContext
                .MapChunks.Where(c => c.ServerId == serverId)
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
                TotalChunks = chunks.Count,
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
            var chunk = await _dbContext.MapChunks.FirstOrDefaultAsync(c =>
                c.ServerId == serverId && c.ChunkX == chunkX && c.ChunkZ == chunkZ
            );

            if (chunk == null)
            {
                _logger.LogWarning(
                    "Chunk ({ChunkX}, {ChunkZ}) not found for server {ServerId}",
                    chunkX,
                    chunkZ,
                    serverId
                );
                return null;
            }

            // Convert entity to StoredChunkData and render
            var chunkData = new StoredChunkData
            {
                ChunkX = chunk.ChunkX,
                ChunkZ = chunk.ChunkZ,
                ContentHash = chunk.ContentHash,
                RainHeightMap = chunk.RainHeightMapData,
                SurfaceBlockId = chunk.SurfaceBlockIdsData,
            };

            var chunkArray = new[] { chunkData };
            var imageBytes = await _mapRendering.RenderGroupedTileAsync(0, 0, chunkArray);

            return imageBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error rendering tile image for chunk ({ChunkX}, {ChunkZ}) on server {ServerId}",
                chunkX,
                chunkZ,
                serverId
            );
            throw;
        }
    }

    public async Task<byte[]?> GetGroupedTileImageAsync(Guid serverId, int groupX, int groupZ)
    {
        try
        {
            // Fetch all chunks for this group from the database
            var chunkList = new List<StoredChunkData>();

            for (var chunkOffsetZ = 0; chunkOffsetZ < 8; chunkOffsetZ++)
            {
                for (var chunkOffsetX = 0; chunkOffsetX < 8; chunkOffsetX++)
                {
                    var chunkX = (groupX * 8) + chunkOffsetX;
                    var chunkZ = (groupZ * 8) + chunkOffsetZ;

                    var entity = await _dbContext.MapChunks.FirstOrDefaultAsync(c =>
                        c.ServerId == serverId && c.ChunkX == chunkX && c.ChunkZ == chunkZ
                    );

                    if (entity != null)
                    {
                        chunkList.Add(
                            new StoredChunkData
                            {
                                ChunkX = entity.ChunkX,
                                ChunkZ = entity.ChunkZ,
                                ContentHash = entity.ContentHash,
                                RainHeightMap = entity.RainHeightMapData,
                                SurfaceBlockId = entity.SurfaceBlockIdsData,
                            }
                        );
                    }
                }
            }

            // Render with the loaded chunks
            var imageBytes = await _mapRendering.RenderGroupedTileAsync(
                groupX,
                groupZ,
                chunkList.ToArray()
            );

            return imageBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error rendering grouped tile image for group ({GroupX}, {GroupZ}) on server {ServerId}",
                groupX,
                groupZ,
                serverId
            );
            throw;
        }
    }

    public async Task<MapTileMetadataDTO?> GetTileMetadataAsync(
        Guid serverId,
        int chunkX,
        int chunkZ
    )
    {
        try
        {
            var chunk = await _dbContext.MapChunks.FirstOrDefaultAsync(c =>
                c.ServerId == serverId && c.ChunkX == chunkX && c.ChunkZ == chunkZ
            );

            if (chunk == null)
            {
                _logger.LogWarning(
                    "Chunk ({ChunkX}, {ChunkZ}) not found for server {ServerId}",
                    chunkX,
                    chunkZ,
                    serverId
                );
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
                ExtractedAt = chunk.ExtractedAt,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving tile metadata for chunk ({ChunkX}, {ChunkZ}) on server {ServerId}",
                chunkX,
                chunkZ,
                serverId
            );
            throw;
        }
    }

    public async Task<byte[]?> GetNotFoundTileImageAsync(Guid serverid, int chunkX, int chunkZ)
    {
        var mapTile = await _mapRendering.GetFogOfWarTileAsync();

        return mapTile;
    }
}
