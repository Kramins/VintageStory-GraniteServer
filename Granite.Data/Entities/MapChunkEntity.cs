using System;

namespace GraniteServer.Data.Entities;

/// <summary>
/// Represents stored map chunk data extracted from a Vintage Story world.
/// </summary>
public class MapChunkEntity
{
    /// <summary>
    /// Unique identifier for this record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The server this chunk belongs to.
    /// </summary>
    public Guid ServerId { get; set; }

    /// <summary>
    /// Chunk X coordinate.
    /// </summary>
    public int ChunkX { get; set; }

    /// <summary>
    /// Chunk Z coordinate.
    /// </summary>
    public int ChunkZ { get; set; }

    /// <summary>
    /// SHA256 hash of the chunk content for change detection.
    /// </summary>
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>
    /// Rain height map data (32x32 int values).
    /// </summary>
    public int[] RainHeightMapData { get; set; } = [];

    /// <summary>
    /// Surface block IDs data (32x32 int values).
    /// </summary>
    public int[] SurfaceBlockIdsData { get; set; } = [];

    /// <summary>
    /// When the chunk data was extracted from the game.
    /// </summary>
    public DateTime ExtractedAt { get; set; }

    /// <summary>
    /// When the chunk was received by the server.
    /// </summary>
    public DateTime ReceivedAt { get; set; }

    /// <summary>
    /// When the chunk data was last accessed for rendering.
    /// Used for cache eviction.
    /// </summary>
    public DateTime? LastAccessedAt { get; set; }

    /// <summary>
    /// Navigation property to the server.
    /// </summary>
    public virtual ServerEntity? Server { get; set; }
}
