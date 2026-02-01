using System;
using System.Threading.Tasks;
using GraniteServer.Data;
using GraniteServer.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Granite.Server.Services.Map;

/// <summary>
/// Service for storing and retrieving map chunk data from the database.
/// </summary>
public interface IMapDataStorageService
{
    /// <summary>
    /// Stores chunk data in the database.
    /// Returns true if the data was new or updated, false if unchanged.
    /// </summary>
    Task<bool> StoreChunkDataAsync(
        Guid serverId,
        int chunkX,
        int chunkZ,
        string contentHash,
        int[] rainHeightMap,
        int[] surfaceBlockIds,
        DateTime extractedAt
    );

    /// <summary>
    /// Gets the hash for a chunk if it exists.
    /// </summary>
    Task<string?> GetChunkHashAsync(Guid serverId, int chunkX, int chunkZ);

    /// <summary>
    /// Gets raw chunk data for rendering.
    /// </summary>
    Task<StoredChunkData?> GetChunkDataAsync(Guid serverId, int chunkX, int chunkZ);

    /// <summary>
    /// Gets all chunk hashes for a server.
    /// </summary>
    Task<IReadOnlyList<StoredChunkHash>> GetAllChunkHashesAsync(Guid serverId);

    /// <summary>
    /// Deletes chunks for a server that haven't been accessed recently.
    /// </summary>
    Task<int> CleanupOldChunksAsync(Guid serverId, TimeSpan maxAge);
}

/// <summary>
/// Stored chunk data for rendering.
/// </summary>
public record StoredChunkData(
    int ChunkX,
    int ChunkZ,
    string ContentHash,
    int[] RainHeightMap,
    int[] SurfaceBlockIds
);

/// <summary>
/// Stored chunk hash for sync.
/// </summary>
public record StoredChunkHash(int ChunkX, int ChunkZ, string ContentHash);

/// <summary>
/// Implementation of IMapDataStorageService using EF Core.
/// </summary>
public class MapDataStorageService : IMapDataStorageService
{
    private readonly GraniteDataContext _dbContext;
    private readonly ILogger<MapDataStorageService> _logger;

    public MapDataStorageService(
        GraniteDataContext dbContext,
        ILogger<MapDataStorageService> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public async Task<string?> GetChunkHashAsync(Guid serverId, int chunkX, int chunkZ)
    {
        var hash = await _dbContext
            .MapChunks.Where(c =>
                c.ServerId == serverId && c.ChunkX == chunkX && c.ChunkZ == chunkZ
            )
            .Select(c => c.ContentHash)
            .FirstOrDefaultAsync();

        return hash;
    }

    /// <inheritdoc/>
    public async Task<StoredChunkData?> GetChunkDataAsync(Guid serverId, int chunkX, int chunkZ)
    {
        var entity = await _dbContext.MapChunks.FirstOrDefaultAsync(c =>
            c.ServerId == serverId && c.ChunkX == chunkX && c.ChunkZ == chunkZ
        );

        if (entity == null)
            return null;

        // Update last accessed time
        entity.LastAccessedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return new StoredChunkData(
            entity.ChunkX,
            entity.ChunkZ,
            entity.ContentHash,
            entity.RainHeightMapData,
            entity.SurfaceBlockIdsData
        );
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<StoredChunkHash>> GetAllChunkHashesAsync(Guid serverId)
    {
        var hashes = await _dbContext
            .MapChunks.Where(c => c.ServerId == serverId)
            .Select(c => new StoredChunkHash(c.ChunkX, c.ChunkZ, c.ContentHash))
            .ToListAsync();

        return hashes;
    }

    /// <inheritdoc/>
    public async Task<int> CleanupOldChunksAsync(Guid serverId, TimeSpan maxAge)
    {
        var cutoff = DateTime.UtcNow - maxAge;

        var oldChunks = await _dbContext
            .MapChunks.Where(c =>
                c.ServerId == serverId && (c.LastAccessedAt == null || c.LastAccessedAt < cutoff)
            )
            .ToListAsync();

        if (oldChunks.Count == 0)
            return 0;

        _dbContext.MapChunks.RemoveRange(oldChunks);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Cleaned up {Count} old chunks for server {ServerId}",
            oldChunks.Count,
            serverId
        );

        return oldChunks.Count;
    }

    /// <summary>
    /// Converts block IDs to byte array for storage.
    /// </summary>
    public static byte[] BlockIdsToBytes(int[] blockIds)
    {
        var bytes = new byte[blockIds.Length * sizeof(int)];
        Buffer.BlockCopy(blockIds, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    /// <summary>
    /// Converts byte array back to block IDs.
    /// </summary>
    public static int[] BytesToBlockIds(byte[] data)
    {
        var blockIds = new int[data.Length / sizeof(int)];
        Buffer.BlockCopy(data, 0, blockIds, 0, data.Length);
        return blockIds;
    }
}
