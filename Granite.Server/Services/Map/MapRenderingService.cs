using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using GraniteServer.Map;
using Microsoft.Extensions.Logging;

namespace Granite.Server.Services.Map;

/// <summary>
/// Service for rendering map tiles from raw chunk data.
/// </summary>
public interface IMapRenderingService
{
    /// <summary>
    /// Renders a single chunk tile from stored data.
    /// </summary>
    /// <param name="serverId">Server ID</param>
    /// <param name="chunkX">Chunk X coordinate</param>
    /// <param name="chunkZ">Chunk Z coordinate</param>
    /// <param name="blockIdToColorCode">Mapping of block IDs to color codes</param>
    /// <returns>PNG image bytes or null if chunk data not available</returns>
    Task<byte[]?> RenderChunkTileAsync(
        Guid serverId,
        int chunkX,
        int chunkZ,
        IReadOnlyDictionary<int, string>? blockIdToColorCode = null
    );

    /// <summary>
    /// Renders a tile from pre-loaded chunk data (no database lookup).
    /// </summary>
    byte[] RenderTileFromData(
        StoredChunkData chunkData,
        IReadOnlyDictionary<int, string>? blockIdToColorCode = null,
        int mapHeightForShading = 256
    );

    /// <summary>
    /// Clears the rendered tile cache for a server.
    /// </summary>
    void InvalidateCache(Guid serverId);

    /// <summary>
    /// Clears the rendered tile cache for a specific chunk.
    /// </summary>
    void InvalidateChunk(Guid serverId, int chunkX, int chunkZ);
}

/// <summary>
/// Implementation of IMapRenderingService.
/// </summary>
public class MapRenderingService : IMapRenderingService
{
    private readonly IMapDataStorageService _storageService;
    private readonly ILogger<MapRenderingService> _logger;

    // Cache for rendered tiles: key = "serverId:chunkX:chunkZ", value = (hash, pngBytes)
    private readonly ConcurrentDictionary<string, (string hash, byte[] data)> _tileCache = new();

    /// <summary>
    /// Chunk size in blocks (32x32).
    /// </summary>
    public const int ChunkSize = 32;

    public MapRenderingService(
        IMapDataStorageService storageService,
        ILogger<MapRenderingService> logger
    )
    {
        _storageService = storageService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<byte[]?> RenderChunkTileAsync(
        Guid serverId,
        int chunkX,
        int chunkZ,
        IReadOnlyDictionary<int, string>? blockIdToColorCode = null
    )
    {
        var cacheKey = $"{serverId}:{chunkX}:{chunkZ}";

        // Check cache first
        if (_tileCache.TryGetValue(cacheKey, out var cached))
        {
            // Verify the hash still matches
            var currentHash = await _storageService.GetChunkHashAsync(serverId, chunkX, chunkZ);
            if (currentHash == cached.hash)
            {
                _logger.LogDebug(
                    "Returning cached tile for chunk ({ChunkX}, {ChunkZ})",
                    chunkX,
                    chunkZ
                );
                return cached.data;
            }
        }

        // Get chunk data
        var chunkData = await _storageService.GetChunkDataAsync(serverId, chunkX, chunkZ);
        if (chunkData == null)
        {
            _logger.LogDebug(
                "No data available for chunk ({ChunkX}, {ChunkZ})",
                chunkX,
                chunkZ
            );
            return null;
        }

        // Render the tile
        var tileBytes = RenderTileFromData(chunkData, blockIdToColorCode);

        // Cache the result
        _tileCache[cacheKey] = (chunkData.ContentHash, tileBytes);

        _logger.LogDebug(
            "Rendered and cached tile for chunk ({ChunkX}, {ChunkZ})",
            chunkX,
            chunkZ
        );

        return tileBytes;
    }

    /// <inheritdoc/>
    public byte[] RenderTileFromData(
        StoredChunkData chunkData,
        IReadOnlyDictionary<int, string>? blockIdToColorCode = null,
        int mapHeightForShading = 256
    )
    {
        var pixels = new uint[ChunkSize * ChunkSize];
        var mapYHalf = mapHeightForShading / 2f;

        for (var localZ = 0; localZ < ChunkSize; localZ++)
        {
            for (var localX = 0; localX < ChunkSize; localX++)
            {
                var index = localZ * ChunkSize + localX;
                var height = chunkData.RainHeightMap[index];
                var blockId = chunkData.SurfaceBlockIds[index];

                // Get color for block
                uint color;
                if (blockIdToColorCode != null && blockIdToColorCode.TryGetValue(blockId, out var colorCode))
                {
                    color = MapColors.GetColor(colorCode);
                }
                else
                {
                    // Default to land color if no mapping available
                    color = MapColors.GetColor("land");
                }

                // Apply height-based shading
                var heightFactor = Math.Clamp(height / mapYHalf, 0.5f, 1.5f);
                color = MapColors.ApplyBrightness(color, heightFactor);

                pixels[index] = color;
            }
        }

        return EncodeToPng(pixels, ChunkSize, ChunkSize);
    }

    /// <inheritdoc/>
    public void InvalidateCache(Guid serverId)
    {
        var prefix = $"{serverId}:";
        var keysToRemove = _tileCache.Keys.Where(k => k.StartsWith(prefix)).ToList();

        foreach (var key in keysToRemove)
        {
            _tileCache.TryRemove(key, out _);
        }

        _logger.LogInformation(
            "Invalidated {Count} cached tiles for server {ServerId}",
            keysToRemove.Count,
            serverId
        );
    }

    /// <inheritdoc/>
    public void InvalidateChunk(Guid serverId, int chunkX, int chunkZ)
    {
        var cacheKey = $"{serverId}:{chunkX}:{chunkZ}";
        _tileCache.TryRemove(cacheKey, out _);
    }

    /// <summary>
    /// Encodes pixel data to PNG format.
    /// Currently outputs TGA for simplicity - can be upgraded to proper PNG.
    /// </summary>
    private static byte[] EncodeToPng(uint[] pixels, int width, int height)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write a simple TGA file (uncompressed true-color)
        // Header
        writer.Write((byte)0); // ID length
        writer.Write((byte)0); // Color map type
        writer.Write((byte)2); // Image type (uncompressed true-color)
        writer.Write((short)0); // Color map origin
        writer.Write((short)0); // Color map length
        writer.Write((byte)0); // Color map depth
        writer.Write((short)0); // X origin
        writer.Write((short)0); // Y origin
        writer.Write((short)width);
        writer.Write((short)height);
        writer.Write((byte)32); // Bits per pixel
        writer.Write((byte)0x28); // Image descriptor (top-left origin, 8-bit alpha)

        // Write pixel data (BGRA format for TGA)
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pixel = pixels[y * width + x];
                writer.Write((byte)(pixel & 0xFF)); // B
                writer.Write((byte)((pixel >> 8) & 0xFF)); // G
                writer.Write((byte)((pixel >> 16) & 0xFF)); // R
                writer.Write((byte)((pixel >> 24) & 0xFF)); // A
            }
        }

        return ms.ToArray();
    }
}
