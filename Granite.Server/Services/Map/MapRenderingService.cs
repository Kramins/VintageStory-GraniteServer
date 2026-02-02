using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using GraniteServer.Map;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

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
    /// Renders a grouped tile (256×256 pixels) from 8×8 chunks with fog of war for missing chunks.
    /// </summary>
    /// <param name="serverId">Server ID</param>
    /// <param name="groupX">Group X coordinate (leftmost chunk X)</param>
    /// <param name="groupZ">Group Z coordinate (topmost chunk Z)</param>
    /// <param name="blockIdToColorCode">Mapping of block IDs to color codes</param>
    /// <returns>PNG image bytes or null if no chunks found</returns>
    Task<byte[]?> RenderGroupedTileAsync(
        Guid serverId,
        int groupX,
        int groupZ,
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

    public async Task<byte[]?> RenderGroupedTileAsync(
        Guid serverId,
        int groupX,
        int groupZ,
        IReadOnlyDictionary<int, string>? blockIdToColorCode = null
    )
    {
        const int ChunksPerGroup = 8;
        const int GroupedTileSize = ChunkSize * ChunksPerGroup; // 256×256 pixels

        // ✅ No snapping needed - groupX and groupZ are already group coordinates
        var cacheKey = $"{serverId}:grouped:{groupX}:{groupZ}";

        // Check cache first
        if (_tileCache.TryGetValue(cacheKey, out var cached))
        {
            _logger.LogDebug(
                "Returning cached grouped tile for group ({GroupX}, {GroupZ})",
                groupX,
                groupZ
            );
            return cached.data;
        }

        // Fog of war color (RGBA: 64,64,64,153 ≈ 60% opacity)
        const uint FogOfWarColor = 0x99404040;
        var pixels = new uint[GroupedTileSize * GroupedTileSize];

        // Fill with fog of war
        Array.Fill(pixels, FogOfWarColor);

        bool anyChunkLoaded = false;

        for (var chunkOffsetZ = 0; chunkOffsetZ < ChunksPerGroup; chunkOffsetZ++)
        {
            for (var chunkOffsetX = 0; chunkOffsetX < ChunksPerGroup; chunkOffsetX++)
            {
                // 🌍 Calculate actual chunk coordinates from group coordinates
                var chunkX = (groupX * ChunksPerGroup) + chunkOffsetX;
                var chunkZ = (groupZ * ChunksPerGroup) + chunkOffsetZ;

                var chunkData = await _storageService.GetChunkDataAsync(serverId, chunkX, chunkZ);

                if (chunkData == null)
                    continue;

                anyChunkLoaded = true;

                // 🧱 Pixel offsets inside the grouped tile
                var pixelOffsetX = chunkOffsetX * ChunkSize;
                var pixelOffsetY = chunkOffsetZ * ChunkSize;

                var mapYHalf = 256 / 2f;

                for (var localZ = 0; localZ < ChunkSize; localZ++)
                {
                    for (var localX = 0; localX < ChunkSize; localX++)
                    {
                        var chunkIndex = localZ * ChunkSize + localX;
                        var height = chunkData.RainHeightMap[chunkIndex];
                        var blockId = chunkData.SurfaceBlockIds[chunkIndex];

                        uint color;
                        if (
                            blockIdToColorCode != null
                            && blockIdToColorCode.TryGetValue(blockId, out var colorCode)
                        )
                        {
                            color = MapColors.GetColor(colorCode);
                        }
                        else
                        {
                            color = MapColors.GetColor("land");
                        }

                        // Height shading
                        var heightFactor = Math.Clamp(height / mapYHalf, 0.5f, 1.5f);
                        color = MapColors.ApplyBrightness(color, heightFactor);

                        var groupedPixelX = pixelOffsetX + localX;
                        var groupedPixelY = pixelOffsetY + localZ;
                        var groupedIndex = groupedPixelY * GroupedTileSize + groupedPixelX;

                        pixels[groupedIndex] = color;
                    }
                }
            }
        }

        if (!anyChunkLoaded)
        {
            _logger.LogDebug(
                "No chunk data available for grouped tile ({GroupX}, {GroupZ})",
                groupX,
                groupZ
            );
            return null;
        }

        var tileBytes = EncodeToPng(pixels, GroupedTileSize, GroupedTileSize);

        // Cache with the actual group coordinates
        var groupHash = $"group_{groupX}_{groupZ}";
        _tileCache[cacheKey] = (groupHash, tileBytes);

        _logger.LogDebug(
            "Rendered and cached grouped tile for group ({GroupX}, {GroupZ}), containing chunks ({ChunkXStart}-{ChunkXEnd}, {ChunkZStart}-{ChunkZEnd})",
            groupX,
            groupZ,
            groupX * ChunksPerGroup,
            (groupX * ChunksPerGroup) + ChunksPerGroup - 1,
            groupZ * ChunksPerGroup,
            (groupZ * ChunksPerGroup) + ChunksPerGroup - 1
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
    /// Encodes pixel data to PNG format using ImageSharp.
    /// </summary>
    private static byte[] EncodeToPng(uint[] pixels, int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pixel = pixels[y * width + x];
                image[x, y] = new Rgba32(
                    (byte)((pixel >> 16) & 0xFF), // R
                    (byte)((pixel >> 8) & 0xFF),  // G
                    (byte)(pixel & 0xFF),         // B
                    (byte)((pixel >> 24) & 0xFF)  // A
                );
            }
        }

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }
}
