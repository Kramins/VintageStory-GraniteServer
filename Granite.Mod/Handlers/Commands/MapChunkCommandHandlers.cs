using Granite.Mod.Services.Map;
using GraniteServer.Messaging.Commands;
using GraniteServer.Messaging.Events;
using GraniteServer.Messaging.Handlers.Commands;
using GraniteServer.Services;
using Vintagestory.API.Common;

namespace GraniteServer.Mod.Handlers.Commands;

public class MapChunkCommandHandlers
    : ICommandHandler<RequestMapChunkDataCommand>,
        ICommandHandler<RequestMapChunkHashesCommand>,
        ICommandHandler<SyncMapCommand>
{
    private readonly IMapDataExtractionService _mapService;
    private readonly ClientMessageBusService _messageBus;
    private readonly GraniteModConfig _config;
    private readonly ILogger _logger;

    public MapChunkCommandHandlers(
        IMapDataExtractionService mapService,
        ClientMessageBusService messageBus,
        GraniteModConfig config,
        ILogger logger
    )
    {
        _mapService = mapService;
        _messageBus = messageBus;
        _config = config;
        _logger = logger;
    }

    async Task ICommandHandler<RequestMapChunkDataCommand>.Handle(
        RequestMapChunkDataCommand command
    )
    {
        if (!_mapService.IsAvailable)
        {
            _logger.Warning("[MapChunkHandler] Map service not available");
            return;
        }

        var chunks = command.Data?.Chunks ?? [];
        _logger.Debug($"[MapChunkHandler] Received request for {chunks.Count} chunks");

        foreach (var coord in chunks)
        {
            var chunkData = await _mapService.ExtractChunkDataAsync(coord.ChunkX, coord.ChunkZ);
            if (chunkData == null)
            {
                _logger.Debug(
                    $"[MapChunkHandler] Chunk ({coord.ChunkX}, {coord.ChunkZ}) not available"
                );
                continue;
            }

            var chunkEvent = _messageBus.CreateEvent<MapChunkDataEvent>(
                _config.ServerId,
                e =>
                {
                    e.Data = new MapChunkDataEventData
                    {
                        ChunkX = chunkData.ChunkX,
                        ChunkZ = chunkData.ChunkZ,
                        ContentHash = chunkData.ContentHash,
                        RainHeightMap = chunkData.RainHeightMap.Select(i => (int)i).ToArray(),
                        SurfaceBlockIds = chunkData.SurfaceBlockIds,
                        ExtractedAt = DateTime.UtcNow,
                    };
                }
            );

            _messageBus.Publish(chunkEvent);
        }
    }

    async Task ICommandHandler<RequestMapChunkHashesCommand>.Handle(
        RequestMapChunkHashesCommand command
    )
    {
        if (!_mapService.IsAvailable)
        {
            _logger.Warning("[MapChunkHandler] Map service not available");
            return;
        }

        var data = command.Data;
        if (data == null)
        {
            _logger.Warning("[MapChunkHandler] Command data is null");
            return;
        }

        _logger.Debug(
            $"[MapChunkHandler] Extracting hashes for region around ({data.CenterChunkX}, {data.CenterChunkZ}) with radius {data.RadiusInChunks}"
        );

        var hashes = await _mapService.ExtractChunkHashesAsync(
            data.CenterChunkX,
            data.CenterChunkZ,
            data.RadiusInChunks
        );

        var hashesEvent = _messageBus.CreateEvent<MapChunkHashesEvent>(
            _config.ServerId,
            e =>
            {
                e.Data = new MapChunkHashesEventData
                {
                    ChunkHashes = hashes
                        .Select(h => new ChunkHashInfo
                        {
                            ChunkX = h.ChunkX,
                            ChunkZ = h.ChunkZ,
                            ContentHash = h.ContentHash,
                        })
                        .ToList(),
                };
            }
        );

        _messageBus.Publish(hashesEvent);
        _logger.Debug($"[MapChunkHandler] Published {hashes.Count} chunk hashes");
    }

    async Task ICommandHandler<SyncMapCommand>.Handle(SyncMapCommand command)
    {
        if (!_mapService.IsAvailable)
        {
            _logger.Warning("[MapChunkHandler] Map service not available, skipping sync");
            return;
        }

        var knownChunks = command.Data?.KnownChunks ?? [];
        _logger.Debug(
            $"[MapChunkHandler] Received SyncMapCommand with {knownChunks.Count} known chunks from server"
        );

        // Build a dictionary of known chunks by coordinate for quick lookup
        var knownChunkDict = knownChunks.ToDictionary(
            c => (c.ChunkX, c.ChunkZ),
            c => c.ContentHash
        );

        // Get all locally extracted chunks
        var localChunks = await _mapService.GetAllExtractedChunksAsync();
        _logger.Debug($"[MapChunkHandler] Found {localChunks.Count} locally extracted chunks");

        // Find chunks that are new or have changed hashes
        var chunksToSend = new List<(int ChunkX, int ChunkZ)>();

        foreach (var localChunk in localChunks)
        {
            var chunkCoord = (localChunk.ChunkX, localChunk.ChunkZ);

            if (!knownChunkDict.ContainsKey(chunkCoord))
            {
                // New chunk not on server
                chunksToSend.Add(chunkCoord);
                _logger.Debug($"[MapChunkHandler] Chunk {chunkCoord} is new");
            }
            else if (knownChunkDict[chunkCoord] != localChunk.ContentHash)
            {
                // Chunk has changed
                chunksToSend.Add(chunkCoord);
                _logger.Debug(
                    $"[MapChunkHandler] Chunk {chunkCoord} has changed hash (old: {knownChunkDict[chunkCoord][..8]}, new: {localChunk.ContentHash[..8]})"
                );
            }
        }

        _logger.Debug($"[MapChunkHandler] Sending {chunksToSend.Count} chunks to server");

        // Extract and send data for all chunks that need to be synced
        foreach (var (chunkX, chunkZ) in chunksToSend)
        {
            var chunkData = await _mapService.ExtractChunkDataAsync(chunkX, chunkZ);
            if (chunkData == null)
            {
                _logger.Debug(
                    $"[MapChunkHandler] Could not extract data for chunk ({chunkX}, {chunkZ})"
                );
                continue;
            }

            var chunkEvent = _messageBus.CreateEvent<MapChunkDataEvent>(
                _config.ServerId,
                e =>
                {
                    e.Data = new MapChunkDataEventData
                    {
                        ChunkX = chunkData.ChunkX,
                        ChunkZ = chunkData.ChunkZ,
                        ContentHash = chunkData.ContentHash,
                        RainHeightMap = chunkData.RainHeightMap.Select(i => (int)i).ToArray(),
                        SurfaceBlockIds = chunkData.SurfaceBlockIds,
                        ExtractedAt = DateTime.UtcNow,
                    };
                }
            );

            _messageBus.Publish(chunkEvent);
        }

        _logger.Notification(
            $"[MapChunkHandler] SyncMapCommand complete - sent {chunksToSend.Count} chunks"
        );
    }

    Task ICommandHandler.Handle(object command)
    {
        throw new NotImplementedException();
    }
}
