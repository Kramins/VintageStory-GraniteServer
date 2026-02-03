namespace GraniteServer.Messaging.Events;

/// <summary>
/// Event sent from Granite.Mod to Granite.Server containing raw map chunk data.
/// This data will be stored and rendered by the server.
/// </summary>
public class MapChunkDataEvent : EventMessage<MapChunkDataEventData> { }

/// <summary>
/// Raw map chunk data extracted from Vintage Story world.
/// </summary>
public class MapChunkDataEventData
{
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
    /// Calculated from RainHeightMap + SurfaceBlockIds.
    /// </summary>
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>
    /// Rain height map (32x32 = 1024 ushort values).
    /// Height of topmost rain-permeable block for each x,z coordinate.
    /// </summary>
    public int[] RainHeightMap { get; set; } = [];

    /// <summary>
    /// Block IDs at surface positions (32x32 = 1024 int values).
    /// </summary>
    public int[] SurfaceBlockIds { get; set; } = [];

    /// <summary>
    /// Timestamp when chunk data was extracted.
    /// </summary>
    public DateTime ExtractedAt { get; set; }
}
