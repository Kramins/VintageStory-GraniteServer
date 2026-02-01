namespace GraniteServer.Messaging.Events;

/// <summary>
/// Event sent from Granite.Mod to Granite.Server containing chunk hashes for a region.
/// Used for efficient change detection - server can compare hashes to determine which chunks need updating.
/// </summary>
public class MapChunkHashesEvent : EventMessage<MapChunkHashesEventData> { }

/// <summary>
/// Collection of chunk hashes for efficient sync.
/// </summary>
public class MapChunkHashesEventData
{
    /// <summary>
    /// List of chunk hash information.
    /// </summary>
    public List<ChunkHashInfo> ChunkHashes { get; set; } = [];
}

/// <summary>
/// Hash information for a single chunk.
/// </summary>
public class ChunkHashInfo
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
    /// SHA256 hash of the chunk content.
    /// </summary>
    public string ContentHash { get; set; } = string.Empty;
}
