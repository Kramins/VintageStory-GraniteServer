using System;
using System.Threading.Tasks;
using Granite.Server.Models;
using GraniteServer.Map;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Granite.Server.Services.Map;

/// <summary>
/// Service for rendering map tiles from raw chunk data.
/// Pure rendering layer with no database or storage dependencies.
/// </summary>
public interface IMapRenderingService
{
    /// <summary>
    /// Renders a grouped tile (256×256 pixels) from pre-loaded chunks with fog of war for missing chunks.
    /// </summary>
    /// <param name="groupX">Group X coordinate</param>
    /// <param name="groupZ">Group Z coordinate</param>
    /// <param name="chunks">Pre-loaded chunk data to render</param>
    /// <param name="blockIdToColorCode">Mapping of block IDs to color codes</param>
    /// <returns>PNG image bytes or null if no chunks provided</returns>
    Task<byte[]?> RenderGroupedTileAsync(
        int groupX,
        int groupZ,
        StoredChunkData[] chunks,
        IReadOnlyDictionary<int, string>? blockIdToColorCode = null
    );

    /// <summary>
    /// Gets a fog of war tile image (for missing tiles).
    /// </summary>
    Task<byte[]> GetFogOfWarTileAsync();
}

/// <summary>
/// Implementation of IMapRenderingService.
/// Pure rendering service with no database or storage dependencies.
/// </summary>
public class MapRenderingService : IMapRenderingService
{
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
    /// Default map height for shading calculations.
    /// </summary>
    private const int DefaultMapHeight = 256;

    public MapRenderingService(ILogger<MapRenderingService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<byte[]?> RenderGroupedTileAsync(
        int groupX,
        int groupZ,
        StoredChunkData[] chunks,
        IReadOnlyDictionary<int, string>? blockIdToColorCode = null
    )
    {
        if (chunks == null || chunks.Length == 0)
        {
            _logger.LogDebug("No chunks provided for grouped tile ({GroupX}, {GroupZ})", groupX, groupZ);
            return Task.FromResult<byte[]?>(null);
        }

        // Create tile with fog of war background
        var pixels = CreateFogOfWarTile(GroupedTileSize);

        // Create a lookup for quick chunk access by coordinates
        var chunkLookup = new Dictionary<(int, int), StoredChunkData>();
        foreach (var chunk in chunks)
        {
            chunkLookup[(chunk.ChunkX, chunk.ChunkZ)] = chunk;
        }

        var loadedChunks = 0;

        // Render each chunk in the group
        for (var chunkOffsetZ = 0; chunkOffsetZ < ChunksPerGroup; chunkOffsetZ++)
        {
            for (var chunkOffsetX = 0; chunkOffsetX < ChunksPerGroup; chunkOffsetX++)
            {
                // Calculate actual chunk coordinates from group coordinates
                var chunkX = (groupX * ChunksPerGroup) + chunkOffsetX;
                var chunkZ = (groupZ * ChunksPerGroup) + chunkOffsetZ;

                if (!chunkLookup.TryGetValue((chunkX, chunkZ), out var chunkData))
                    continue;

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

        if (loadedChunks == 0)
        {
            _logger.LogDebug(
                "No chunk data available for grouped tile ({GroupX}, {GroupZ})",
                groupX,
                groupZ
            );
            return Task.FromResult<byte[]?>(null);
        }

        // Encode to PNG
        var tileBytes = EncodeToPng(pixels, GroupedTileSize, GroupedTileSize);

        _logger.LogDebug(
            "Rendered grouped tile for group ({GroupX}, {GroupZ}): {LoadedChunks}/{TotalChunks} chunks",
            groupX,
            groupZ,
            loadedChunks,
            ChunksPerGroup * ChunksPerGroup
        );

        return Task.FromResult<byte[]?>(tileBytes);
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

        if (chunkData.RainHeightMap == null || chunkData.SurfaceBlockId == null)
            return;

        for (var localZ = 0; localZ < ChunkSize; localZ++)
        {
            for (var localX = 0; localX < ChunkSize; localX++)
            {
                var chunkIndex = localZ * ChunkSize + localX;
                var height = chunkData.RainHeightMap[chunkIndex];
                var blockId = chunkData.SurfaceBlockId[chunkIndex];

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
    public Task<byte[]> GetFogOfWarTileAsync()
    {
        // Create fog of war tile
        var pixels = CreateFogOfWarTile(GroupedTileSize);
        var tileBytes = EncodeToPng(pixels, GroupedTileSize, GroupedTileSize);

        return Task.FromResult(tileBytes);
    }

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
}
