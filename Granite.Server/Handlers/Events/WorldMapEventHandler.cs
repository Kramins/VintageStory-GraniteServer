using System.Threading.Tasks;
using Granite.Common.Messaging.Events.Client;
using Granite.Server.Services;
using Granite.Server.Services.Map;
using GraniteServer.Messaging.Events;
using Microsoft.Extensions.Logging;

namespace GraniteServer.Messaging.Handlers.Events;

/// <summary>
/// Handles MapChunkDataEvent - stores raw chunk data received from the mod.
/// </summary>
public class WorldMapEventHandler
    : IEventHandler<MapChunkDataEvent>,
        IEventHandler<MapChunkHashesEvent>
{
    private readonly IServerWorldMapService _worldMapService;
    private readonly IMapRenderingService _renderingService;
    private readonly PersistentMessageBusService _messageBus;
    private readonly ILogger<WorldMapEventHandler> _logger;

    public WorldMapEventHandler(
        IServerWorldMapService worldMapService,
        IMapRenderingService renderingService,
        PersistentMessageBusService messageBus,
        ILogger<WorldMapEventHandler> logger
    )
    {
        _worldMapService = worldMapService;
        _renderingService = renderingService;
        _messageBus = messageBus;
        _logger = logger;
    }

    async Task IEventHandler<MapChunkDataEvent>.Handle(MapChunkDataEvent @event)
    {
        var data = @event.Data;
        if (data == null)
        {
            _logger.LogWarning("Received MapChunkDataEvent with null data");
            return;
        }

        var serverId = @event.OriginServerId;

        _logger.LogDebug(
            "Received chunk data for ({ChunkX}, {ChunkZ}) from server {ServerId}, hash: {Hash}",
            data.ChunkX,
            data.ChunkZ,
            serverId,
            data.ContentHash[..Math.Min(8, data.ContentHash.Length)]
        );

        // Store the chunk data
        var wasUpdated = await _worldMapService.StoreChunkDataAsync(
            serverId,
            data.ChunkX,
            data.ChunkZ,
            data.ContentHash,
            data.RainHeightMap,
            data.SurfaceBlockIds,
            data.ExtractedAt
        );

        if (wasUpdated)
        {
            // Convert chunk coordinates to tile coordinates (groups of 8x8 chunks)
            int tileX = (int)Math.Floor((double)data.ChunkX / 8);
            int tileZ = (int)Math.Floor((double)data.ChunkZ / 8);

            var mapTileUpdatedEvent = _messageBus.CreateEvent<MapTileUpdatedEvent>(
                @event.OriginServerId,
                e =>
                {
                    e.Data = new MapTileUpdateEventData { TileX = tileX, TileZ = tileZ };
                }
            );

            _messageBus.Publish(mapTileUpdatedEvent);
            _logger.LogInformation(
                "Stored chunk ({ChunkX}, {ChunkZ}) for server {ServerId}",
                data.ChunkX,
                data.ChunkZ,
                serverId
            );
        }
    }

    async Task IEventHandler<MapChunkHashesEvent>.Handle(MapChunkHashesEvent @event)
    {
        var data = @event.Data;
        if (data == null)
        {
            _logger.LogWarning("Received MapChunkHashesEvent with null data");
            return;
        }

        var serverId = @event.OriginServerId;
        var chunkHashes = data.ChunkHashes;

        _logger.LogInformation(
            "Received {Count} chunk hashes from server {ServerId}",
            chunkHashes.Count,
            serverId
        );

        // Get our stored hashes for comparison
        var storedHashes = await _worldMapService.GetAllChunkHashesAsync(serverId);
        var storedHashLookup = storedHashes.ToDictionary(
            h => (h.ChunkX, h.ChunkZ),
            h => h.ContentHash
        );

        // Find chunks that are new or changed
        var newOrChangedChunks = new List<ChunkHashInfo>();
        foreach (var chunk in chunkHashes)
        {
            if (
                !storedHashLookup.TryGetValue((chunk.ChunkX, chunk.ChunkZ), out var storedHash)
                || storedHash != chunk.ContentHash
            )
            {
                newOrChangedChunks.Add(chunk);
            }
        }

        _logger.LogInformation(
            "Found {Count} new or changed chunks out of {Total} received",
            newOrChangedChunks.Count,
            chunkHashes.Count
        );

        // Note: At this point, we could request the full data for changed chunks
        // by publishing a RequestMapChunkDataCommand, but that's handled by
        // higher-level sync logic (e.g., a MapSyncService)
    }

    Task IEventHandler.Handle(object command)
    {
        throw new NotImplementedException();
    }
}
