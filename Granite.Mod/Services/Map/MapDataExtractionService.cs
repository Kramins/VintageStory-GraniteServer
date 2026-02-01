using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Granite.Mod.Services.Map;

/// <summary>
/// Service for extracting raw map data from Vintage Story world.
/// Extracts heights and block IDs - rendering is done server-side.
/// </summary>
public class MapDataExtractionService : IMapDataExtractionService
{
    private readonly ICoreServerAPI _api;
    private readonly ILogger _logger;
    private bool _isInitialized;

    /// <summary>
    /// Chunk size in blocks (32x32).
    /// </summary>
    public const int ChunkSizeConst = 32;

    public MapDataExtractionService(ICoreServerAPI api, ILogger logger)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public int MapSizeX => _api.World?.BlockAccessor?.MapSizeX ?? 0;

    /// <inheritdoc/>
    public int MapSizeZ => _api.World?.BlockAccessor?.MapSizeZ ?? 0;

    /// <inheritdoc/>
    public int ChunkSize => GlobalConstants.ChunkSize;

    /// <inheritdoc/>
    public int SpawnX => (int)(_api.World?.DefaultSpawnPosition?.X ?? MapSizeX / 2);

    /// <inheritdoc/>
    public int SpawnZ => (int)(_api.World?.DefaultSpawnPosition?.Z ?? MapSizeZ / 2);

    /// <inheritdoc/>
    public bool IsAvailable => true;//_api.World != null && _isInitialized;

    /// <summary>
    /// Initializes the service. Should be called after world is loaded.
    /// </summary>
    public void Initialize()
    {
        if (_api.World?.Blocks == null)
        {
            _logger.Warning(
                "[MapDataExtraction] World blocks not available, deferring initialization"
            );
            return;
        }

        _isInitialized = true;
        _logger.Notification("[MapDataExtraction] Service initialized");
    }

    /// <inheritdoc/>
    public Task<MapChunkExtractedData?> ExtractChunkDataAsync(
        int chunkX,
        int chunkZ,
        CancellationToken cancellationToken = default
    )
    {
        if (!IsAvailable)
        {
            _logger.Debug("[MapDataExtraction] Service not available");
            return Task.FromResult<MapChunkExtractedData?>(null);
        }

        var tcs = new TaskCompletionSource<MapChunkExtractedData?>();

        // Must run on main thread to access world data safely
        _api.Event.EnqueueMainThreadTask(
            () =>
            {
                try
                {
                    var result = ExtractChunkDataInternal(chunkX, chunkZ);
                    tcs.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    _logger.Error(
                        $"[MapDataExtraction] Error extracting chunk ({chunkX}, {chunkZ}): {ex.Message}"
                    );
                    tcs.TrySetResult(null);
                }
            },
            "ExtractMapChunkData"
        );

        return tcs.Task;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<ChunkHashData>> ExtractChunkHashesAsync(
        int centerChunkX,
        int centerChunkZ,
        int radiusInChunks,
        CancellationToken cancellationToken = default
    )
    {
        if (!IsAvailable)
        {
            _logger.Debug("[MapDataExtraction] Service not available");
            return Task.FromResult<IReadOnlyList<ChunkHashData>>([]);
        }

        var tcs = new TaskCompletionSource<IReadOnlyList<ChunkHashData>>();

        _api.Event.EnqueueMainThreadTask(
            () =>
            {
                try
                {
                    var result = ExtractChunkHashesInternal(
                        centerChunkX,
                        centerChunkZ,
                        radiusInChunks
                    );
                    tcs.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    _logger.Error(
                        $"[MapDataExtraction] Error extracting hashes for region ({centerChunkX}, {centerChunkZ}): {ex.Message}"
                    );
                    tcs.TrySetResult([]);
                }
            },
            "ExtractMapChunkHashes"
        );

        return tcs.Task;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<ChunkHashData>> GetAllExtractedChunksAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (!IsAvailable)
        {
            _logger.Debug("[MapDataExtraction] Service not available");
            return Task.FromResult<IReadOnlyList<ChunkHashData>>([]);
        }

        var tcs = new TaskCompletionSource<IReadOnlyList<ChunkHashData>>();

        _api.Event.EnqueueMainThreadTask(
            () =>
            {
                try
                {
                    var result = GetAllExtractedChunksInternal();
                    tcs.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    _logger.Error(
                        $"[MapDataExtraction] Error getting all extracted chunks: {ex.Message}"
                    );
                    tcs.TrySetResult([]);
                }
            },
            "GetAllExtractedChunks"
        );

        return tcs.Task;
    }

    /// <inheritdoc/>
    public MapPositionInfo? GetPositionInfo(int worldX, int worldZ)
    {
        if (!IsAvailable)
            return null;

        var blockAccessor = _api.World.BlockAccessor;
        var chunkX = worldX / ChunkSize;
        var chunkZ = worldZ / ChunkSize;

        var mapChunk = blockAccessor.GetMapChunk(chunkX, chunkZ);
        if (mapChunk?.RainHeightMap == null)
            return null;

        var localX = worldX % ChunkSize;
        var localZ = worldZ % ChunkSize;
        var heightMapIndex = localZ * ChunkSize + localX;
        var height = mapChunk.RainHeightMap[heightMapIndex];

        var blockPos = new BlockPos(worldX, height, worldZ, 0);
        var block = blockAccessor.GetBlock(blockPos, BlockLayersAccess.FluidOrSolid);

        if (block == null)
            return null;

        var colorCode = GetBlockColorCode(block);

        return new MapPositionInfo(
            worldX,
            worldZ,
            height,
            block.Id,
            block.Code?.ToString() ?? "unknown",
            colorCode
        );
    }

    /// <summary>
    /// Internal method to extract chunk data. Must be called on main thread.
    /// </summary>
    private MapChunkExtractedData? ExtractChunkDataInternal(int chunkX, int chunkZ)
    {
        var blockAccessor = _api.World.BlockAccessor;
        var mapChunk = blockAccessor.GetMapChunk(chunkX, chunkZ);

        if (mapChunk?.RainHeightMap == null)
        {
            _logger.Debug(
                $"[MapDataExtraction] Chunk ({chunkX}, {chunkZ}) not loaded or has no heightmap"
            );
            return null;
        }

        var chunkSize = ChunkSize;
        var heightMap = new ushort[chunkSize * chunkSize];
        var surfaceBlockIds = new int[chunkSize * chunkSize];

        // Copy height map
        Array.Copy(mapChunk.RainHeightMap, heightMap, heightMap.Length);

        // Extract surface block IDs
        for (var localZ = 0; localZ < chunkSize; localZ++)
        {
            for (var localX = 0; localX < chunkSize; localX++)
            {
                var heightMapIndex = localZ * chunkSize + localX;
                var height = heightMap[heightMapIndex];

                var worldX = chunkX * chunkSize + localX;
                var worldZ = chunkZ * chunkSize + localZ;

                // Get the block at the rain height
                var blockPos = new BlockPos(worldX, height, worldZ, 0);
                var block = blockAccessor.GetBlock(blockPos, BlockLayersAccess.FluidOrSolid);

                surfaceBlockIds[heightMapIndex] = block?.Id ?? 0;
            }
        }

        // Calculate content hash
        var contentHash = CalculateContentHash(heightMap, surfaceBlockIds);

        return new MapChunkExtractedData(
            chunkX,
            chunkZ,
            contentHash,
            heightMap,
            surfaceBlockIds,
            DateTime.UtcNow
        );
    }

    /// <summary>
    /// Internal method to extract chunk hashes. Must be called on main thread.
    /// </summary>
    private IReadOnlyList<ChunkHashData> ExtractChunkHashesInternal(
        int centerChunkX,
        int centerChunkZ,
        int radiusInChunks
    )
    {
        var results = new List<ChunkHashData>();
        var blockAccessor = _api.World.BlockAccessor;
        var chunkSize = ChunkSize;

        for (
            var chunkZ = centerChunkZ - radiusInChunks;
            chunkZ <= centerChunkZ + radiusInChunks;
            chunkZ++
        )
        {
            for (
                var chunkX = centerChunkX - radiusInChunks;
                chunkX <= centerChunkX + radiusInChunks;
                chunkX++
            )
            {
                var mapChunk = blockAccessor.GetMapChunk(chunkX, chunkZ);
                if (mapChunk?.RainHeightMap == null)
                    continue;

                // Extract data for this chunk to calculate hash
                var heightMap = mapChunk.RainHeightMap;
                var surfaceBlockIds = new int[chunkSize * chunkSize];

                for (var localZ = 0; localZ < chunkSize; localZ++)
                {
                    for (var localX = 0; localX < chunkSize; localX++)
                    {
                        var heightMapIndex = localZ * chunkSize + localX;
                        var height = heightMap[heightMapIndex];

                        var worldX = chunkX * chunkSize + localX;
                        var worldZ = chunkZ * chunkSize + localZ;

                        var blockPos = new BlockPos(worldX, height, worldZ, 0);
                        var block = blockAccessor.GetBlock(blockPos, BlockLayersAccess.FluidOrSolid);
                        surfaceBlockIds[heightMapIndex] = block?.Id ?? 0;
                    }
                }

                var contentHash = CalculateContentHash(heightMap, surfaceBlockIds);
                results.Add(new ChunkHashData(chunkX, chunkZ, contentHash));
            }
        }

        _logger.Debug(
            $"[MapDataExtraction] Extracted {results.Count} chunk hashes for region around ({centerChunkX}, {centerChunkZ})"
        );

        return results;
    }

    /// <summary>
    /// Gets all currently loaded and extracted chunks with their hashes.
    /// </summary>
    private IReadOnlyList<ChunkHashData> GetAllExtractedChunksInternal()
    {
        var results = new List<ChunkHashData>();
        var blockAccessor = _api.World.BlockAccessor;
        var chunkSize = ChunkSize;
        var mapSizeX = MapSizeX;
        var mapSizeZ = MapSizeZ;

        // Calculate chunk dimensions
        var chunksInX = (mapSizeX + chunkSize - 1) / chunkSize;
        var chunksInZ = (mapSizeZ + chunkSize - 1) / chunkSize;

        _logger.Debug(
            $"[MapDataExtraction] Iterating through {chunksInX}x{chunksInZ} chunks for sync"
        );

        // Iterate through all possible chunk coordinates
        for (var chunkZ = 0; chunkZ < chunksInZ; chunkZ++)
        {
            for (var chunkX = 0; chunkX < chunksInX; chunkX++)
            {
                var mapChunk = blockAccessor.GetMapChunk(chunkX, chunkZ);
                if (mapChunk?.RainHeightMap == null)
                    continue;

                // Extract data for this chunk to calculate hash
                var heightMap = mapChunk.RainHeightMap;
                var surfaceBlockIds = new int[chunkSize * chunkSize];

                for (var localZ = 0; localZ < chunkSize; localZ++)
                {
                    for (var localX = 0; localX < chunkSize; localX++)
                    {
                        var heightMapIndex = localZ * chunkSize + localX;
                        var height = heightMap[heightMapIndex];

                        var worldX = chunkX * chunkSize + localX;
                        var worldZ = chunkZ * chunkSize + localZ;

                        var blockPos = new BlockPos(worldX, height, worldZ, 0);
                        var block = blockAccessor.GetBlock(blockPos, BlockLayersAccess.FluidOrSolid);
                        surfaceBlockIds[heightMapIndex] = block?.Id ?? 0;
                    }
                }

                var contentHash = CalculateContentHash(heightMap, surfaceBlockIds);
                results.Add(new ChunkHashData(chunkX, chunkZ, contentHash));
            }
        }

        _logger.Debug(
            $"[MapDataExtraction] Extracted {results.Count} total chunks for sync"
        );

        return results;
    }

    /// <summary>
    /// Gets the color code for a specific block.
    /// </summary>
    public static string GetBlockColorCode(Block block)
    {
        // Special case: snow blocks should be glacier (matches WebCartographer)
        if (
            block.BlockMaterial == EnumBlockMaterial.Snow
            && block.Code?.Path?.Contains("snowblock") == true
        )
        {
            return "glacier";
        }

        // First try to get the mapColorCode attribute from the block
        var colorCode = block.Attributes?["mapColorCode"]?.AsString();
        if (!string.IsNullOrEmpty(colorCode) && MapColors.ColorsByCode.ContainsKey(colorCode))
        {
            return colorCode;
        }

        // Fall back to material-based color
        return MapColors.GetDefaultMapColorCode(block.BlockMaterial);
    }

    /// <summary>
    /// Calculates a SHA256 hash from height map and block IDs for change detection.
    /// </summary>
    public static string CalculateContentHash(ushort[] heightMap, int[] blockIds)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write heightmap
        foreach (var height in heightMap)
            writer.Write(height);

        // Write block IDs
        foreach (var blockId in blockIds)
            writer.Write(blockId);

        writer.Flush();
        var bytes = ms.ToArray();
        var hashBytes = SHA256.HashData(bytes);

        return Convert.ToHexString(hashBytes);
    }
}
