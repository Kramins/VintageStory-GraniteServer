namespace GraniteServer.Messaging.Commands;

/// <summary>
/// Command sent from Granite.Server to Granite.Mod to request chunk hashes for a region.
/// Used for efficient sync - server can compare hashes to determine which chunks have changed.
/// The mod will respond with MapChunkHashesEvent containing hashes for all loaded chunks in the region.
/// </summary>
public class RequestMapChunkHashesCommand : CommandMessage<RequestMapChunkHashesCommandData> { }

/// <summary>
/// Data for requesting chunk hashes in a region.
/// </summary>
public class RequestMapChunkHashesCommandData
{
    /// <summary>
    /// Center chunk X coordinate.
    /// </summary>
    public int CenterChunkX { get; set; }

    /// <summary>
    /// Center chunk Z coordinate.
    /// </summary>
    public int CenterChunkZ { get; set; }

    /// <summary>
    /// Radius in chunks from the center (e.g., 16 = 32x32 chunk area).
    /// </summary>
    public int RadiusInChunks { get; set; }

    /// <summary>
    /// If true, attempt to include all chunks (may trigger chunk loading).
    /// If false, only return currently loaded chunks.
    /// </summary>
    public bool IncludeUnloadedChunks { get; set; } = false;
}
