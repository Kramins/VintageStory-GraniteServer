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
            // Load the target chunk and its neighbors for hill-shading/water-edge detection
            var neighborCoords = new[]
            {
                (chunkX, chunkZ),
                (chunkX - 1, chunkZ - 1), // NW
                (chunkX - 1, chunkZ),       // W
                (chunkX, chunkZ - 1),       // N
                (chunkX + 1, chunkZ),       // E
                (chunkX, chunkZ + 1),       // S
            };

            var entities = await _dbContext
                .MapChunks.Where(c =>
                    c.ServerId == serverId
                    && neighborCoords.Select(n => n.Item1).Contains(c.ChunkX)
                    && neighborCoords.Select(n => n.Item2).Contains(c.ChunkZ)
                )
                .ToListAsync();

            // Filter to only the exact coordinates we need
            var chunkEntities = entities
                .Where(e => neighborCoords.Any(n => n.Item1 == e.ChunkX && n.Item2 == e.ChunkZ))
                .ToList();

            var mainChunk = chunkEntities.FirstOrDefault(c =>
                c.ChunkX == chunkX && c.ChunkZ == chunkZ
            );

            if (mainChunk == null)
            {
                _logger.LogWarning(
                    "Chunk ({ChunkX}, {ChunkZ}) not found for server {ServerId}",
                    chunkX,
                    chunkZ,
                    serverId
                );
                return null;
            }

            var chunkArray = chunkEntities
                .Select(e => new StoredChunkData
                {
                    ChunkX = e.ChunkX,
                    ChunkZ = e.ChunkZ,
                    ContentHash = e.ContentHash,
                    RainHeightMap = e.RainHeightMapData,
                    SurfaceBlockId = e.SurfaceBlockIdsData,
                })
                .ToArray();

            // Load block color mappings from Collectibles
            var (blockIdToColorCode, blockIdToMaterial) =
                await LoadBlockColorMappingsAsync(serverId);

            var imageBytes = await _mapRendering.RenderGroupedTileAsync(
                0,
                0,
                chunkArray,
                blockIdToColorCode,
                blockIdToMaterial
            );

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
            // Calculate chunk coordinate ranges for this group plus 1-chunk border for neighbor shading
            var minChunkX = (groupX * 8) - 1;
            var maxChunkX = (groupX * 8) + 8; // inclusive (border chunk)
            var minChunkZ = (groupZ * 8) - 1;
            var maxChunkZ = (groupZ * 8) + 8; // inclusive (border chunk)

            // Batch-load all chunks in range (including neighbors) in a single query
            var entities = await _dbContext
                .MapChunks.Where(c =>
                    c.ServerId == serverId
                    && c.ChunkX >= minChunkX
                    && c.ChunkX <= maxChunkX
                    && c.ChunkZ >= minChunkZ
                    && c.ChunkZ <= maxChunkZ
                )
                .ToListAsync();

            var chunkArray = entities
                .Select(e => new StoredChunkData
                {
                    ChunkX = e.ChunkX,
                    ChunkZ = e.ChunkZ,
                    ContentHash = e.ContentHash,
                    RainHeightMap = e.RainHeightMapData,
                    SurfaceBlockId = e.SurfaceBlockIdsData,
                })
                .ToArray();

            // Load block color mappings from Collectibles
            var (blockIdToColorCode, blockIdToMaterial) =
                await LoadBlockColorMappingsAsync(serverId);

            // Render with the loaded chunks (renderer will pick the right ones per group position)
            var imageBytes = await _mapRendering.RenderGroupedTileAsync(
                groupX,
                groupZ,
                chunkArray,
                blockIdToColorCode,
                blockIdToMaterial
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

    /// <summary>
    /// Loads block ID to color code and block ID to material mappings from the Collectibles table.
    /// Only loads blocks (not items) since items don't appear on the map.
    /// </summary>
    private async Task<(
        IReadOnlyDictionary<int, string> blockIdToColorCode,
        IReadOnlyDictionary<int, string> blockIdToMaterial
    )> LoadBlockColorMappingsAsync(Guid serverId)
    {
        var blocks = await _dbContext
            .Collectibles.Where(c => c.ServerId == serverId && c.Type == "block")
            .Select(c => new
            {
                c.CollectibleId,
                c.MapColorCode,
                c.BlockMaterial,
            })
            .ToListAsync();

        var colorCodeDict = new Dictionary<int, string>();
        var materialDict = new Dictionary<int, string>();

        foreach (var block in blocks)
        {
            if (!string.IsNullOrEmpty(block.MapColorCode))
            {
                colorCodeDict[block.CollectibleId] = block.MapColorCode;
            }

            if (!string.IsNullOrEmpty(block.BlockMaterial))
            {
                materialDict[block.CollectibleId] = block.BlockMaterial;
            }
        }

        return (colorCodeDict, materialDict);
    }
}
