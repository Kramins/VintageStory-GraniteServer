using System.Threading;
using System.Threading.Tasks;

namespace Granite.Mod.Services.Map;

/// <summary>
/// Service interface for extracting raw map data from Vintage Story world.
/// Data extraction only - rendering is done server-side.
/// </summary>
public interface IMapDataExtractionService
{
    /// <summary>
    /// Gets the map size in blocks for the X axis.
    /// </summary>
    int MapSizeX { get; }

    /// <summary>
    /// Gets the map size in blocks for the Z axis.
    /// </summary>
    int MapSizeZ { get; }

    /// <summary>
    /// Gets the chunk size (typically 32).
    /// </summary>
    int ChunkSize { get; }

    /// <summary>
    /// Gets the default spawn position X coordinate.
    /// </summary>
    int SpawnX { get; }

    /// <summary>
    /// Gets the default spawn position Z coordinate.
    /// </summary>
    int SpawnZ { get; }

    /// <summary>
    /// Checks if data extraction is available (server is ready and world is loaded).
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Extracts raw chunk data for a specific chunk coordinate.
    /// </summary>
    /// <param name="chunkX">Chunk X coordinate</param>
    /// <param name="chunkZ">Chunk Z coordinate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Extracted chunk data or null if chunk is not loaded</returns>
    Task<MapChunkExtractedData?> ExtractChunkDataAsync(
        int chunkX,
        int chunkZ,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Extracts chunk hashes for a region around the specified center.
    /// </summary>
    /// <param name="centerChunkX">Center chunk X coordinate</param>
    /// <param name="centerChunkZ">Center chunk Z coordinate</param>
    /// <param name="radiusInChunks">Radius in chunks from center</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of chunk hashes for loaded chunks in the region</returns>
    Task<IReadOnlyList<ChunkHashData>> ExtractChunkHashesAsync(
        int centerChunkX,
        int centerChunkZ,
        int radiusInChunks,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets all currently extracted chunks with their hashes.
    /// Used for initial sync to determine which chunks to send to the server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all extracted chunks with their hashes</returns>
    Task<IReadOnlyList<ChunkHashData>> GetAllExtractedChunksAsync(
        CancellationToken cancellationToken = default
    );
    string? GetChunkHash(int chunkX, int chunkZ);

    /// <summary>
    /// Gets the block ID for a specific world position.
    /// </summary>
    /// <param name="worldX">World X coordinate</param>
    /// <param name="worldZ">World Z coordinate</param>
    /// <returns>Block information or null if position is not loaded</returns>
    MapPositionInfo? GetPositionInfo(int worldX, int worldZ);
}

/// <summary>
/// Raw chunk data extracted from Vintage Story world.
/// </summary>
public record MapChunkExtractedData(
    int ChunkX,
    int ChunkZ,
    string ContentHash,
    ushort[] RainHeightMap,
    int[] SurfaceBlockIds,
    float AverageTemperature,
    float AverageRainfall,
    DateTime ExtractedAt
);

/// <summary>
/// Hash data for a single chunk.
/// </summary>
public record ChunkHashData(
    int ChunkX,
    int ChunkZ,
    string ContentHash
);

/// <summary>
/// Information about a specific map position.
/// </summary>
public record MapPositionInfo(
    int WorldX,
    int WorldZ,
    int Height,
    int BlockId,
    string BlockCode,
    string ColorCode
);
