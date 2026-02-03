using GraniteServer.Messaging.Events;

namespace GraniteServer.Messaging.Commands;

/// <summary>
/// Command sent from Granite.Server to Granite.Mod when the server is ready to sync map data.
/// Contains all known chunk coordinates and their hashes from the server cache.
/// The mod will compare its locally extracted chunks with these hashes and send back any new or modified chunks.
/// This sync happens once per server startup when the server becomes ready.
/// </summary>
public class SyncMapCommand : CommandMessage<SyncMapCommandData> { }

/// <summary>
/// Data for synchronizing map chunks containing all known chunks on the server.
/// </summary>
public class SyncMapCommandData
{
    /// <summary>
    /// List of all chunks known on the server with their hashes.
    /// The mod should compare these with locally extracted chunks and send back any mismatches.
    /// </summary>
    public List<ChunkHashInfo> KnownChunks { get; set; } = [];
}
