using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Granite.Common.Messaging.Events.Client;
using Granite.Server.Configuration;
using Granite.Server.Services;
using GraniteServer.Messaging.Events;
using Microsoft.Extensions.Options;

namespace Granite.Server.HostedServices;

public class ServerWorldMapHostedService : IHostedService
{
    private IServiceProvider _serviceProvider;
    private PersistentMessageBusService _messageBus;
    private GraniteServerOptions _options;
    private ILogger<ServerWorldMapHostedService> _logger;
    private Subject<Tuple<Guid, int, int>> _mapTileUpdateSubject = new();
    private List<IDisposable> _subscriptions = new();

    public ServerWorldMapHostedService(
        IServiceProvider serviceProvider,
        PersistentMessageBusService messageBus,
        IOptions<GraniteServerOptions> options,
        ILogger<ServerWorldMapHostedService> logger
    )
    {
        _serviceProvider = serviceProvider;
        _messageBus = messageBus;
        _options = options.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _subscriptions.Add(_messageBus.Subscribe<MapChunkDataEvent>(HandleMapChunkDataEvent));
        _subscriptions.Add(_messageBus.Subscribe<MapChunkHashesEvent>(HandleMapChunkHashesEvent));

        _subscriptions.Add(
            _mapTileUpdateSubject
                .AsObservable()
                .GroupBy(x => x.Item1) // Group by ServerId
                .SelectMany(group =>
                    group
                        .Buffer(TimeSpan.FromSeconds(1)) // Buffer updates within each group for 1 second
                        .Where(batch => batch.Count > 0) // Only process if there are updates
                        .Select(batch => (ServerId: group.Key, Tiles: batch))
                )
                .Subscribe(
                    onNext: item => HandleMapTileUpdateBatch(item.ServerId, item.Tiles),
                    onError: ex => _logger.LogError(ex, "Error in map tile update observable"),
                    onCompleted: () => _logger.LogInformation("Map tile update observable completed")
                )
        );

        return Task.CompletedTask;
    }

    private void HandleMapTileUpdateBatch(Guid serverId, IList<Tuple<Guid, int, int>> tiles)
    {
        try
        {
            var updatedTiles = tiles
                .Select(x => new MapTile { TileX = x.Item2, TileZ = x.Item3 })
                .Distinct()
                .ToList();

            var mapTileUpdatedEvent = _messageBus.CreateEvent<MapTilesUpdatedEvent>(
                serverId,
                e =>
                {
                    e.Data = new MapTilesUpdateEventData
                    {
                        ServerId = serverId,
                        UpdatedTiles = updatedTiles,
                    };
                }
            );

            _messageBus.Publish(mapTileUpdatedEvent);
            _logger.LogInformation(
                "Published MapTilesUpdatedEvent for server {ServerId} with {Count} updated tiles",
                serverId,
                updatedTiles.Count
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling map tile update batch for server {ServerId}", serverId);
        }
    }

    private async void HandleMapChunkDataEvent(MapChunkDataEvent @event)
    {
        try
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

            // Create scope for database operations
            using var scope = _serviceProvider.CreateScope();
            var worldMapService =
                scope.ServiceProvider.GetRequiredService<IServerWorldMapService>();

            // Store the chunk data
            var wasUpdated = await worldMapService.StoreChunkDataAsync(
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

                // Emit tile update event for this tile
                _mapTileUpdateSubject.OnNext(Tuple.Create(serverId, tileX, tileZ));
                _logger.LogInformation(
                    "Stored chunk ({ChunkX}, {ChunkZ}) for server {ServerId}",
                    data.ChunkX,
                    data.ChunkZ,
                    serverId
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MapChunkDataEvent");
        }
    }

    private async void HandleMapChunkHashesEvent(MapChunkHashesEvent @event)
    {
        try
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

            // Create scope for database operations
            using var scope = _serviceProvider.CreateScope();
            var worldMapService =
                scope.ServiceProvider.GetRequiredService<IServerWorldMapService>();

            // Get our stored hashes for comparison
            var storedHashes = await worldMapService.GetAllChunkHashesAsync(serverId);
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MapChunkHashesEvent");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _subscriptions.Clear();
        _mapTileUpdateSubject.Dispose();
        return Task.CompletedTask;
    }
}
