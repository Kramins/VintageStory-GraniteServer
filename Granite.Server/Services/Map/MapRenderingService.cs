using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Granite.Server.Models;
using GraniteServer.Map;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Granite.Server.Services.Map;


/*
Review & Recommendations
This file is doing too much in the GenerateChunkImage method (180+ lines). Here are key areas to decompose:

1. Hill-shading calculation (lines 230-265)
Extract into a separate method to reduce cognitive load.

2. Water-edge detection (lines 267-305)
The repetitive water checks for 4 cardinal directions can be extracted.

3. Shadow map processing (lines 310-330)
The blur and modulation logic should be its own method.

4. Neighbor chunk fetching (lines 202-218)
Can be extracted for clarity.
*/

/// <summary>
/// Service for rendering map tiles from raw chunk data.
/// Replicates Vintagestory.GameContent.ChunkMapLayer.GenerateChunkImage()
/// to produce visually accurate map tiles on the control plane.
/// </summary>
public interface IMapRenderingService
{
    /// <summary>
    /// Renders a grouped tile (256x256 pixels) from pre-loaded chunks with fog of war for missing chunks.
    /// Applies hill-shading, water-edge detection matching the game client.
    /// </summary>
    /// <param name="groupX">Group X coordinate</param>
    /// <param name="groupZ">Group Z coordinate</param>
    /// <param name="chunks">Pre-loaded chunk data to render (including neighbor chunks for border shading)</param>
    /// <param name="blockIdToColorCode">Mapping of block IDs to mapColorCode attribute values</param>
    /// <param name="blockIdToMaterial">Mapping of block IDs to BlockMaterial enum string values</param>
    /// <returns>PNG image bytes or null if no chunks provided</returns>
    Task<byte[]?> RenderGroupedTileAsync(
        int groupX,
        int groupZ,
        StoredChunkData[] chunks,
        IReadOnlyDictionary<int, string>? blockIdToColorCode = null,
        IReadOnlyDictionary<int, string>? blockIdToMaterial = null
    );

    /// <summary>
    /// Gets a fog of war tile image (for missing tiles).
    /// </summary>
    Task<byte[]> GetFogOfWarTileAsync();
}

/// <summary>
/// Rendering service that replicates the Vintage Story client's ChunkMapLayer.GenerateChunkImage().
///
/// The game client renders map tiles as follows:
/// 1. For each pixel in a 32x32 chunk, look up the surface block via RainHeightMap
/// 2. Map block ID -> color code -> palette color (medieval style)
/// 3. Compute hill-shading from height differences to NW/W/N neighbor pixels
///    (crossing chunk boundaries when pixel is at x=0 or z=0)
/// 4. Detect water edges: if a water pixel has any non-water cardinal neighbor, use "wateredge" color
/// 5. Apply shadow map: build per-pixel shadow values, box-blur them, then modulate final colors
///
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

    public MapRenderingService(ILogger<MapRenderingService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<byte[]?> RenderGroupedTileAsync(
        int groupX,
        int groupZ,
        StoredChunkData[] chunks,
        IReadOnlyDictionary<int, string>? blockIdToColorCode = null,
        IReadOnlyDictionary<int, string>? blockIdToMaterial = null
    )
    {
        if (chunks == null || chunks.Length == 0)
        {
            _logger.LogDebug(
                "No chunks provided for grouped tile ({GroupX}, {GroupZ})",
                groupX,
                groupZ
            );
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

        // Pre-build a block ID -> color index lookup for all known blocks
        var blockColorIndices = BuildBlockColorIndices(
            blockIdToColorCode,
            blockIdToMaterial,
            chunks
        );

        var loadedChunks = 0;

        // Render each chunk in the group
        for (var chunkOffsetZ = 0; chunkOffsetZ < ChunksPerGroup; chunkOffsetZ++)
        {
            for (var chunkOffsetX = 0; chunkOffsetX < ChunksPerGroup; chunkOffsetX++)
            {
                var chunkX = (groupX * ChunksPerGroup) + chunkOffsetX;
                var chunkZ = (groupZ * ChunksPerGroup) + chunkOffsetZ;

                if (!chunkLookup.TryGetValue((chunkX, chunkZ), out var chunkData))
                    continue;

                // Skip chunks missing critical data; they will show as fog of war
                // This shouldn't happen
                if (chunkData.RainHeightMap == null || chunkData.SurfaceBlockId == null)
                    continue;

                loadedChunks++;

                // Generate the 32x32 chunk image with full shading
                var chunkPixels = GenerateChunkImage(
                    chunkX,
                    chunkZ,
                    chunkData,
                    chunkLookup,
                    blockColorIndices
                );

                // Copy chunk pixels into the grouped tile
                CopyChunkIntoTile(pixels, chunkPixels, chunkOffsetX, chunkOffsetZ);
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
    /// Generates a 32x32 pixel array for a single chunk, replicating the game client's
    /// ChunkMapLayer.GenerateChunkImage() algorithm.
    ///
    /// Key differences from the game client:
    /// - We don't have 3D chunk data, so we use SurfaceBlockIds instead of UnpackAndReadBlock
    /// - Water-edge detection uses SurfaceBlockIds of neighboring pixels instead of 3D block reads
    /// - Snow peek-through is not possible without 3D data; snow blocks retain their glacier color
    /// </summary>
    internal int[] GenerateChunkImage(
        int chunkX,
        int chunkZ,
        StoredChunkData chunkData,
        Dictionary<(int, int), StoredChunkData> chunkLookup,
        Dictionary<int, int> blockColorIndices
    )
    {
        var pixels = new int[ChunkSize * ChunkSize];
        var heightMap = chunkData.RainHeightMap!;
        var blockIds = chunkData.SurfaceBlockId!;

        // Get neighbor chunk heightmaps for hill-shading at pixel borders.
        // The game fetches: NW (chunkX-1, chunkZ-1), W (chunkX-1, chunkZ), N (chunkX, chunkZ-1)
        // These are only used when a pixel is at x=0 or z=0 and needs
        // the height of the adjacent pixel which lives in the neighbor chunk.
        chunkLookup.TryGetValue((chunkX - 1, chunkZ - 1), out var nwChunk);
        chunkLookup.TryGetValue((chunkX - 1, chunkZ), out var wChunk);
        chunkLookup.TryGetValue((chunkX, chunkZ - 1), out var nChunk);

        // Also need E and S neighbors for water-edge detection at chunk borders
        chunkLookup.TryGetValue((chunkX + 1, chunkZ), out var eChunk);
        chunkLookup.TryGetValue((chunkX, chunkZ + 1), out var sChunk);

        var nwHeightMap = nwChunk?.RainHeightMap;
        var wHeightMap = wChunk?.RainHeightMap;
        var nHeightMap = nChunk?.RainHeightMap;

        // Shadow map: initialized to 128 (neutral), modulated by hill-shade factor per pixel.
        var shadowMap = new byte[ChunkSize * ChunkSize];
        Array.Fill(shadowMap, (byte)128);

        // Pre-resolve color palette as int array (matching game's colors[] array)
        var colorKeys = MapColors.ColorsByCode.Keys.ToArray();
        var colorValues = new int[colorKeys.Length];
        for (var i = 0; i < colorKeys.Length; i++)
        {
            colorValues[i] = (int)MapColors.ColorsByCode[colorKeys[i]];
        }
        var wateredgeColorInt = (int)MapColors.GetColor("wateredge");

        // --- Pass 1: Assign base colors and compute shadow values ---
        for (var idx = 0; idx < ChunkSize * ChunkSize; idx++)
        {
            var height = heightMap[idx];
            var blockId = blockIds[idx];

            // Convert linear index to (x, z) pixel coordinates within the chunk
            var x = idx % ChunkSize;
            var z = idx / ChunkSize;

            // --- Hill-shading ---
            float shadeFactor = ComputeHillShadeFactor(
                x,
                z,
                height,
                heightMap,
                nwHeightMap,
                wHeightMap,
                nHeightMap
            );

            // Resolve block color index
            var colorIndex = blockColorIndices.GetValueOrDefault(blockId, 0);
            var isWater = IsWaterByColorIndex(colorIndex, colorKeys);

            // --- Water-edge detection ---
            // The game checks 4 cardinal neighbor pixels (not chunks).
            // If any cardinal neighbor pixel is non-water, use "wateredge" color.
            if (isWater)
            {
                bool wIsWater = IsWaterAtPixel(
                    x - 1,
                    z,
                    blockIds,
                    wChunk?.SurfaceBlockId,
                    eChunk?.SurfaceBlockId,
                    nChunk?.SurfaceBlockId,
                    sChunk?.SurfaceBlockId,
                    blockColorIndices,
                    colorKeys
                );
                bool eIsWater = IsWaterAtPixel(
                    x + 1,
                    z,
                    blockIds,
                    wChunk?.SurfaceBlockId,
                    eChunk?.SurfaceBlockId,
                    nChunk?.SurfaceBlockId,
                    sChunk?.SurfaceBlockId,
                    blockColorIndices,
                    colorKeys
                );
                bool nIsWater = IsWaterAtPixel(
                    x,
                    z - 1,
                    blockIds,
                    wChunk?.SurfaceBlockId,
                    eChunk?.SurfaceBlockId,
                    nChunk?.SurfaceBlockId,
                    sChunk?.SurfaceBlockId,
                    blockColorIndices,
                    colorKeys
                );
                bool sIsWater = IsWaterAtPixel(
                    x,
                    z + 1,
                    blockIds,
                    wChunk?.SurfaceBlockId,
                    eChunk?.SurfaceBlockId,
                    nChunk?.SurfaceBlockId,
                    sChunk?.SurfaceBlockId,
                    blockColorIndices,
                    colorKeys
                );

                if (wIsWater && eIsWater && nIsWater && sIsWater)
                {
                    pixels[idx] = colorValues[colorIndex];
                }
                else
                {
                    pixels[idx] = wateredgeColorInt;
                }
                // Water pixels don't get hill-shading in the game client
            }
            else
            {
                // Non-water: apply hill-shade to shadow map
                shadowMap[idx] = (byte)Math.Clamp((int)(shadowMap[idx] * shadeFactor), 0, 255);
                pixels[idx] = colorValues[colorIndex];
            }
        }

        // --- Pass 2: Shadow blur and final color modulation ---
        ApplyShadowBlurAndModulation(pixels, shadowMap);

        return pixels;
    }

    /// <summary>
    /// Computes the hill-shading factor for a pixel based on height differences with neighbor pixels.
    /// The game compares the current pixel's height to 3 neighbor pixels:
    ///   NW pixel (x-1, z-1), W pixel (x-1, z), N pixel (x, z-1)
    /// When x=0 the W/NW pixel is at x=31 in the west neighbor chunk.
    /// When z=0 the N/NW pixel is at z=31 in the north neighbor chunk.
    /// </summary>
    /// <returns>Shade factor to apply to the shadow map (1.0 = neutral, &gt;1.0 = lighter, &lt;1.0 = darker)</returns>
    private static float ComputeHillShadeFactor(
        int x,
        int z,
        int currentHeight,
        int[] heightMap,
        int[]? nwHeightMap,
        int[]? wHeightMap,
        int[]? nHeightMap
    )
    {
        float shadeFactor = 1f;

        int nwPixelHeight = GetNeighborPixelHeight(
            x - 1,
            z - 1,
            heightMap,
            nwHeightMap,
            wHeightMap,
            nHeightMap,
            currentHeight
        );
        int wPixelHeight = GetNeighborPixelHeight(
            x - 1,
            z,
            heightMap,
            null,
            wHeightMap,
            null,
            currentHeight
        );
        int nPixelHeight = GetNeighborPixelHeight(
            x,
            z - 1,
            heightMap,
            null,
            null,
            nHeightMap,
            currentHeight
        );

        int nwDiff = currentHeight - nwPixelHeight;
        int wDiff = currentHeight - wPixelHeight;
        int nDiff = currentHeight - nPixelHeight;

        // Sum the signs of the 3 height differences
        float signSum = Math.Sign(nwDiff) + Math.Sign(wDiff) + Math.Sign(nDiff);
        float maxAbsDiff = Math.Max(
            Math.Max(Math.Abs(nwDiff), Math.Abs(wDiff)),
            Math.Abs(nDiff)
        );

        if (signSum > 0f)
        {
            shadeFactor = 1.08f + Math.Min(0.5f, maxAbsDiff / 10f) / 1.25f;
        }
        if (signSum < 0f)
        {
            shadeFactor = 0.92f - Math.Min(0.5f, maxAbsDiff / 10f) / 1.25f;
        }

        return shadeFactor;
    }

    /// <summary>
    /// Gets the height of a neighbor pixel for hill-shading.
    /// When the neighbor pixel coordinate is outside the current chunk (x &lt; 0 or z &lt; 0),
    /// reads from the appropriate neighbor chunk's heightmap.
    ///
    /// The game client logic (from GenerateChunkImage):
    /// - When both x &lt; 0 and z &lt; 0: read from NW chunk heightmap
    /// - When only x &lt; 0: read from W chunk heightmap
    /// - When only z &lt; 0: read from N chunk heightmap
    /// - Otherwise: read from current chunk heightmap
    /// Coordinates are wrapped with modulo 32 to index into the neighbor array.
    /// </summary>
    private static int GetNeighborPixelHeight(
        int x,
        int z,
        int[] currentHeightMap,
        int[]? nwHeightMap,
        int[]? wHeightMap,
        int[]? nHeightMap,
        int fallbackHeight
    )
    {
        int[]? targetMap = currentHeightMap;

        if (x < 0 && z < 0)
        {
            targetMap = nwHeightMap;
        }
        else if (x < 0)
        {
            targetMap = wHeightMap;
        }
        else if (z < 0)
        {
            targetMap = nHeightMap;
        }

        if (targetMap == null)
            return fallbackHeight;

        // Wrap coordinates into 0-31 range (equivalent to GameMath.Mod)
        var wrappedX = ((x % ChunkSize) + ChunkSize) % ChunkSize;
        var wrappedZ = ((z % ChunkSize) + ChunkSize) % ChunkSize;
        var idx = wrappedZ * ChunkSize + wrappedX;

        if (idx < 0 || idx >= targetMap.Length)
            return fallbackHeight;

        return targetMap[idx];
    }

    /// <summary>
    /// Checks whether the pixel at (x, z) is a water block.
    /// When the pixel coordinate is outside the current chunk boundaries [0,31],
    /// reads from the appropriate neighbor chunk's SurfaceBlockIds.
    /// </summary>
    private static bool IsWaterAtPixel(
        int x,
        int z,
        int[] currentBlockIds,
        int[]? wBlockIds,
        int[]? eBlockIds,
        int[]? nBlockIds,
        int[]? sBlockIds,
        Dictionary<int, int> blockColorIndices,
        string[] colorKeys
    )
    {
        int[]? targetIds = currentBlockIds;

        if (x < 0)
            targetIds = wBlockIds;
        else if (x >= ChunkSize)
            targetIds = eBlockIds;
        else if (z < 0)
            targetIds = nBlockIds;
        else if (z >= ChunkSize)
            targetIds = sBlockIds;

        if (targetIds == null)
            return true; // Assume water if neighbor chunk is missing (matches game behavior)

        var wrappedX = ((x % ChunkSize) + ChunkSize) % ChunkSize;
        var wrappedZ = ((z % ChunkSize) + ChunkSize) % ChunkSize;
        var idx = wrappedZ * ChunkSize + wrappedX;

        if (idx < 0 || idx >= targetIds.Length)
            return true;

        var blockId = targetIds[idx];
        var colorIndex = blockColorIndices.GetValueOrDefault(blockId, 0);
        return IsWaterByColorIndex(colorIndex, colorKeys);
    }

    /// <summary>
    /// Determines if a color index represents water.
    /// Matches the game's isLake() which returns true for Liquid and Ice (except glacierice).
    /// Since we map Liquid -> "lake" and Ice -> "glacier", we check for "lake" and "ocean".
    /// </summary>
    private static bool IsWaterByColorIndex(int colorIndex, string[] colorKeys)
    {
        if (colorIndex < 0 || colorIndex >= colorKeys.Length)
            return false;

        var code = colorKeys[colorIndex];
        return code == "lake" || code == "ocean";
    }

    /// <summary>
    /// Applies shadow map blur and final color modulation to the pixel array.
    /// The game copies shadowMap before blur, then blends both for the final multiplier.
    /// </summary>
    private static void ApplyShadowBlurAndModulation(int[] pixels, byte[] shadowMap)
    {
        var shadowMapPreBlur = new byte[shadowMap.Length];
        Array.Copy(shadowMap, shadowMapPreBlur, shadowMap.Length);

        BlurTool.Blur(shadowMap, ChunkSize, ChunkSize, 2);

        for (var i = 0; i < pixels.Length; i++)
        {
            float blurredVal = (int)((shadowMap[i] / 128f - 1f) * 5f) / 5f;
            float preBlurVal = ((shadowMapPreBlur[i] / 128f - 1f) * 5f) % 1f / 5f;
            float combined = blurredVal + preBlurVal;

            pixels[i] =
                MapColors.ColorMultiply3Clamped(pixels[i], combined + 1f)
                | unchecked((int)0xFF000000);
        }
    }

    /// <summary>
    /// Builds a mapping from block ID to color palette index for all blocks
    /// seen in the provided chunks.
    /// </summary>
    internal static Dictionary<int, int> BuildBlockColorIndices(
        IReadOnlyDictionary<int, string>? blockIdToColorCode,
        IReadOnlyDictionary<int, string>? blockIdToMaterial,
        StoredChunkData[] chunks
    )
    {
        var colorKeys = MapColors.ColorsByCode.Keys.ToArray();
        var keyToIndex = new Dictionary<string, int>();
        for (var i = 0; i < colorKeys.Length; i++)
        {
            keyToIndex[colorKeys[i]] = i;
        }

        var landIndex = keyToIndex.GetValueOrDefault("land", 0);
        var result = new Dictionary<int, int>();

        // Collect all unique block IDs from chunks
        var allBlockIds = new HashSet<int>();
        foreach (var chunk in chunks)
        {
            if (chunk.SurfaceBlockId == null)
                continue;
            foreach (var bid in chunk.SurfaceBlockId)
            {
                allBlockIds.Add(bid);
            }
        }

        // Resolve each block ID to a color index
        foreach (var blockId in allBlockIds)
        {
            var colorCode = MapColors.ResolveColorCode(
                blockId,
                blockIdToColorCode,
                blockIdToMaterial
            );
            result[blockId] = keyToIndex.GetValueOrDefault(colorCode, landIndex);
        }

        return result;
    }

    /// <summary>
    /// Copies a 32x32 chunk pixel array into the correct position in the grouped tile.
    /// </summary>
    private static void CopyChunkIntoTile(
        uint[] tilePixels,
        int[] chunkPixels,
        int chunkOffsetX,
        int chunkOffsetZ
    )
    {
        var pixelOffsetX = chunkOffsetX * ChunkSize;
        var pixelOffsetY = chunkOffsetZ * ChunkSize;

        for (var z = 0; z < ChunkSize; z++)
        {
            for (var x = 0; x < ChunkSize; x++)
            {
                var srcIdx = z * ChunkSize + x;
                var dstIdx = (pixelOffsetY + z) * GroupedTileSize + (pixelOffsetX + x);
                tilePixels[dstIdx] = (uint)chunkPixels[srcIdx];
            }
        }
    }

    /// <inheritdoc/>
    public Task<byte[]> GetFogOfWarTileAsync()
    {
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
        var maxDist = (float)Math.Sqrt(2) * center;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var index = y * size + x;
                var dx = x - center;
                var dy = y - center;
                var dist = (float)Math.Sqrt(dx * dx + dy * dy);
                var distFactor = dist / maxDist;

                var noise =
                    Math.Sin(x * 0.1) * Math.Cos(y * 0.1) * 0.3
                    + Math.Sin(x * 0.05 + 100) * Math.Cos(y * 0.05 + 100) * 0.2;

                var baseValue = 45 - (distFactor * 15);
                var value = baseValue + noise * 8;

                var r = (byte)Math.Clamp(value, 25, 55);
                var g = (byte)Math.Clamp(value + 2, 27, 57);
                var b = (byte)Math.Clamp(value + 5, 30, 60);
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
                    (byte)((pixel >> 16) & 0xFF),
                    (byte)((pixel >> 8) & 0xFF),
                    (byte)(pixel & 0xFF),
                    (byte)((pixel >> 24) & 0xFF)
                );
            }
        }

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }
}
