namespace GraniteServer.Messaging.Commands;

/// <summary>
/// Command sent from Granite.Server to Granite.Mod to request raw chunk data for specific chunks.
/// The mod will respond with MapChunkDataEvent(s) for each requested chunk.
/// </summary>
public class RequestMapChunkDataCommand : CommandMessage<RequestMapChunkDataCommandData> { }

/// <summary>
/// Data for requesting specific chunks.
/// </summary>
public class RequestMapChunkDataCommandData
{
    /// <summary>
    /// List of specific chunk coordinates to request full data for.
    /// </summary>
    public List<ChunkCoordinate> Chunks { get; set; } = [];
}

/// <summary>
/// Simple chunk coordinate pair.
/// </summary>
public class ChunkCoordinate
{
    /// <summary>
    /// Chunk X coordinate.
    /// </summary>
    public int ChunkX { get; set; }

    /// <summary>
    /// Chunk Z coordinate.
    /// </summary>
    public int ChunkZ { get; set; }
}
