using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Granite.Common.Messaging.Common;
using GraniteServer.Map;
using Microsoft.Extensions.Caching.Memory;
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
    /// <param name="groupX">Group X coordinate</param>
    /// <param name="groupZ">Group Z coordinate</param>
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
    /// Clears the rendered tile cache for a specific chunk.
    /// Also invalidates the grouped tile that contains this chunk.
    /// </summary>
    void InvalidateChunk(Guid serverId, int chunkX, int chunkZ);

    /// <summary>
    /// Converts chunk coordinates to tile (group) coordinates.
    /// </summary>
    MapTileCoords ChunkCoordsToTileCoords(int chunkX, int chunkZ);

    /// <summary>
    /// Gets a cached "not found" tile image (fog of war).
    /// </summary>
    Task<byte[]> GetNotFoundTileImageAsync();
}

/// <summary>
/// Implementation of IMapRenderingService.
/// </summary>
public class MapRenderingService : IMapRenderingService
{
    private readonly IMapDataStorageService _storageService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<MapRenderingService> _logger;

    /// <summary>
    /// Chunk size in blocks (32x32).
    /// </summary>
    public const int ChunkSize = 32;

    /// <summary>
    /// Number of chunks per grouped tile dimension (8x8 = 64 chunks per tile).
    /// </summary>
    private const int ChunksPerGroup = 8;

    /// <summary>
    /// Grouped tile size in pixels (256x256).
    /// </summary>
    private const int GroupedTileSize = ChunkSize * ChunksPerGroup;

    /// <summary>
    /// Fog of war color (RGBA: 64,64,64,153 ≈ 60% opacity).
    /// </summary>
    private const uint FogOfWarColor = 0x99404040;

    /// <summary>
    /// Default map height for shading calculations.
    /// </summary>
    private const int DefaultMapHeight = 256;

    public MapRenderingService(
        IMapDataStorageService storageService,
        IMemoryCache cache,
        ILogger<MapRenderingService> logger
    )
    {
        _storageService = storageService;
        _cache = cache;
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
        var cacheKey = GetChunkCacheKey(serverId, chunkX, chunkZ);

        // Check cache first
        if (_cache.TryGetValue<(string hash, byte[] data)>(cacheKey, out var cached))
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

            // Hash changed, invalidate cache
            _logger.LogDebug(
                "Hash mismatch for chunk ({ChunkX}, {ChunkZ}), re-rendering",
                chunkX,
                chunkZ
            );
            _cache.Remove(cacheKey);
        }

        // Get chunk data
        var chunkData = await _storageService.GetChunkDataAsync(serverId, chunkX, chunkZ);
        if (chunkData == null)
        {
            _logger.LogDebug("No data available for chunk ({ChunkX}, {ChunkZ})", chunkX, chunkZ);
            return null;
        }

        // Render the tile
        var tileBytes = RenderTileFromData(chunkData, blockIdToColorCode);

        // Cache the result with sliding expiration
        var cacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(1),
            Size = 1, // For size-based eviction if configured
        };

        _cache.Set(cacheKey, (chunkData.ContentHash, tileBytes), cacheOptions);

        _logger.LogDebug("Rendered and cached tile for chunk ({ChunkX}, {ChunkZ})", chunkX, chunkZ);

        return tileBytes;
    }

    /// <inheritdoc/>
    public async Task<byte[]?> RenderGroupedTileAsync(
        Guid serverId,
        int groupX,
        int groupZ,
        IReadOnlyDictionary<int, string>? blockIdToColorCode = null
    )
    {
        var cacheKey = GetGroupCacheKey(serverId, groupX, groupZ);

        // Check cache first
        if (_cache.TryGetValue<byte[]>(cacheKey, out var cachedTile))
        {
            _logger.LogDebug(
                "Returning cached grouped tile for group ({GroupX}, {GroupZ})",
                groupX,
                groupZ
            );
            return cachedTile;
        }

        // Create tile with fog of war background
        var pixels = CreateFogOfWarTile(GroupedTileSize);

        bool anyChunkLoaded = false;
        var loadedChunks = 0;

        // Load and render each chunk in the group
        for (var chunkOffsetZ = 0; chunkOffsetZ < ChunksPerGroup; chunkOffsetZ++)
        {
            for (var chunkOffsetX = 0; chunkOffsetX < ChunksPerGroup; chunkOffsetX++)
            {
                // Calculate actual chunk coordinates from group coordinates
                var chunkX = (groupX * ChunksPerGroup) + chunkOffsetX;
                var chunkZ = (groupZ * ChunksPerGroup) + chunkOffsetZ;

                var chunkData = await _storageService.GetChunkDataAsync(serverId, chunkX, chunkZ);

                if (chunkData == null)
                    continue;

                anyChunkLoaded = true;
                loadedChunks++;

                // Render chunk into the grouped tile
                RenderChunkIntoGroupedTile(
                    pixels,
                    chunkData,
                    chunkOffsetX,
                    chunkOffsetZ,
                    blockIdToColorCode
                );
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

        // Encode to PNG
        var tileBytes = EncodeToPng(pixels, GroupedTileSize, GroupedTileSize);

        // Cache with sliding expiration
        var cacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(1),
            Size = 1,
        };

        _cache.Set(cacheKey, tileBytes, cacheOptions);

        _logger.LogDebug(
            "Rendered and cached grouped tile for group ({GroupX}, {GroupZ}): {LoadedChunks}/{TotalChunks} chunks loaded",
            groupX,
            groupZ,
            loadedChunks,
            ChunksPerGroup * ChunksPerGroup
        );

        return tileBytes;
    }

    /// <summary>
    /// Renders a single chunk into a grouped tile's pixel array.
    /// </summary>
    private void RenderChunkIntoGroupedTile(
        uint[] pixels,
        StoredChunkData chunkData,
        int chunkOffsetX,
        int chunkOffsetZ,
        IReadOnlyDictionary<int, string>? blockIdToColorCode
    )
    {
        var pixelOffsetX = chunkOffsetX * ChunkSize;
        var pixelOffsetY = chunkOffsetZ * ChunkSize;
        var mapYHalf = DefaultMapHeight / 2f;

        for (var localZ = 0; localZ < ChunkSize; localZ++)
        {
            for (var localX = 0; localX < ChunkSize; localX++)
            {
                var chunkIndex = localZ * ChunkSize + localX;
                var height = chunkData.RainHeightMap[chunkIndex];
                var blockId = chunkData.SurfaceBlockIds[chunkIndex];

                // Get block color
                var color = GetBlockColor(blockId, blockIdToColorCode);

                // Apply height-based shading
                var heightFactor = Math.Clamp(height / mapYHalf, 0.5f, 1.5f);
                color = MapColors.ApplyBrightness(color, heightFactor);

                // Write to grouped tile
                var groupedPixelX = pixelOffsetX + localX;
                var groupedPixelY = pixelOffsetY + localZ;
                var groupedIndex = groupedPixelY * GroupedTileSize + groupedPixelX;

                pixels[groupedIndex] = color;
            }
        }
    }

    /// <inheritdoc/>
    public byte[] RenderTileFromData(
        StoredChunkData chunkData,
        IReadOnlyDictionary<int, string>? blockIdToColorCode = null,
        int mapHeightForShading = DefaultMapHeight
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

                // Get block color
                var color = GetBlockColor(blockId, blockIdToColorCode);

                // Apply height-based shading
                var heightFactor = Math.Clamp(height / mapYHalf, 0.5f, 1.5f);
                color = MapColors.ApplyBrightness(color, heightFactor);

                pixels[index] = color;
            }
        }

        return EncodeToPng(pixels, ChunkSize, ChunkSize);
    }

    /// <summary>
    /// Gets the color for a block based on its ID.
    /// </summary>
    private static uint GetBlockColor(
        int blockId,
        IReadOnlyDictionary<int, string>? blockIdToColorCode
    )
    {
        if (
            blockIdToColorCode != null
            && blockIdToColorCode.TryGetValue(blockId, out var colorCode)
        )
        {
            return MapColors.GetColor(colorCode);
        }

        // Default to land color
        return MapColors.GetColor("land");
    }

    /// <inheritdoc/>
    public void InvalidateChunk(Guid serverId, int chunkX, int chunkZ)
    {
        // Invalidate individual chunk cache
        var chunkCacheKey = GetChunkCacheKey(serverId, chunkX, chunkZ);
        _cache.Remove(chunkCacheKey);

        // Invalidate the grouped tile that contains this chunk
        var tileCoords = ChunkCoordsToTileCoords(chunkX, chunkZ);
        var groupCacheKey = GetGroupCacheKey(serverId, tileCoords.TileX, tileCoords.TileZ);
        _cache.Remove(groupCacheKey);

        _logger.LogDebug(
            "Invalidated chunk ({ChunkX}, {ChunkZ}) and group tile ({GroupX}, {GroupZ})",
            chunkX,
            chunkZ,
            tileCoords.TileX,
            tileCoords.TileZ
        );
    }

    /// <inheritdoc/>
    public MapTileCoords ChunkCoordsToTileCoords(int chunkX, int chunkZ)
    {
        // Use floor division to handle negative coordinates correctly
        int tileX = (int)Math.Floor((double)chunkX / ChunksPerGroup);
        int tileZ = (int)Math.Floor((double)chunkZ / ChunksPerGroup);
        return new MapTileCoords(tileX, tileZ);
    }

    /// <inheritdoc/>
    public Task<byte[]> GetNotFoundTileImageAsync()
    {
        const string cacheKey = "notfound:tile";

        if (_cache.TryGetValue<byte[]>(cacheKey, out var cachedImage))
        {
            return Task.FromResult(cachedImage);
        }

        // Create fog of war tile
        var pixels = CreateFogOfWarTile(GroupedTileSize);
        var tileBytes = EncodeToPng(pixels, GroupedTileSize, GroupedTileSize);

        // Cache permanently (this never changes)
        var cacheOptions = new MemoryCacheEntryOptions { Priority = CacheItemPriority.NeverRemove };

        _cache.Set(cacheKey, tileBytes, cacheOptions);

        return Task.FromResult(tileBytes);
    }

    /// <summary>
    /// Creates a tile filled with fog of war color.
    /// </summary>
    /// <summary>
    /// Creates an attractive fog of war tile with vignette edges and subtle texture.
    /// </summary>
    private static uint[] CreateFogOfWarTile(int size)
    {
        var pixels = new uint[size * size];
        var center = size / 2f;
        var maxDist = (float)Math.Sqrt(2) * center; // Corner distance

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var index = y * size + x;

                // Distance from center for vignette effect
                var dx = x - center;
                var dy = y - center;
                var dist = (float)Math.Sqrt(dx * dx + dy * dy);
                var distFactor = dist / maxDist; // 0 at center, 1 at corners

                // Subtle noise for texture
                var noise =
                    Math.Sin(x * 0.1) * Math.Cos(y * 0.1) * 0.3
                    + Math.Sin(x * 0.05 + 100) * Math.Cos(y * 0.05 + 100) * 0.2;

                // Dark at edges, slightly lighter at center
                var baseValue = 45 - (distFactor * 15); // 45 center -> 30 edges
                var value = baseValue + noise * 8;

                var r = (byte)Math.Clamp(value, 25, 55);
                var g = (byte)Math.Clamp(value + 2, 27, 57); // Slight blue tint
                var b = (byte)Math.Clamp(value + 5, 30, 60);

                // More opaque at edges (looks like fog is thicker)
                var alpha = (byte)Math.Clamp(160 + distFactor * 30, 160, 200);

                pixels[index] = ((uint)alpha << 24) | ((uint)r << 16) | ((uint)g << 8) | b;
            }
        }

        return pixels;
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
                    (byte)((pixel >> 8) & 0xFF), // G
                    (byte)(pixel & 0xFF), // B
                    (byte)((pixel >> 24) & 0xFF) // A
                );
            }
        }

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Generates cache key for individual chunk tiles.
    /// </summary>
    private static string GetChunkCacheKey(Guid serverId, int chunkX, int chunkZ) =>
        $"{serverId}:chunk:{chunkX}:{chunkZ}";

    /// <summary>
    /// Generates cache key for grouped tiles.
    /// </summary>
    private static string GetGroupCacheKey(Guid serverId, int groupX, int groupZ) =>
        $"{serverId}:group:{groupX}:{groupZ}";
}
