using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Threading.Channels;
using Granite.Common.Messaging.Events;
using Granite.Mod.Services.Map;
using GraniteServer.Messaging.Commands;
using GraniteServer.Messaging.Events;
using GraniteServer.Mod;
using GraniteServer.Services;
using Microsoft.Extensions.Hosting;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

public class WorldMapHostedService : IHostedService, IDisposable
{
    const int chunksize = GlobalConstants.ChunkSize;
    private int _regionSize;
    private ICoreServerAPI _api;
    private readonly IMapDataExtractionService _mapDataExtractionService;
    private ClientMessageBusService _messageBus;
    private GraniteModConfig _config;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<(int ChunkX, int ChunkZ), string> _chunkHashes =
        new ConcurrentDictionary<(int, int), string>();
    private readonly Channel<(int chunkX, int chunkZ)> _chunkQueue;
    private CancellationTokenSource _cts;
    private IDisposable _syncSubscription;
    private IDisposable _playerJoinedSubscription;
    private Task _processingTask;
    private Task _processingPlayerPositionsTask;
    private bool _isReadyToSendMapChunks;
    private TimeSpan _playerPositionUpdateInterval = TimeSpan.FromSeconds(1);
    private float _playerPositionMovementThreshold = 0.1f; // Minimum movement in blocks to trigger an update
    private readonly TaskCompletionSource<bool> _readyToSendMapChunksTcs =
        new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

    public WorldMapHostedService(
        ICoreServerAPI api,
        IMapDataExtractionService mapDataExtractionService,
        ClientMessageBusService messageBus,
        GraniteModConfig config,
        ILogger logger
    )
    {
        _api = api;
        _mapDataExtractionService = mapDataExtractionService;
        _messageBus = messageBus;
        _config = config;
        _logger = logger;
        _regionSize = _api.WorldManager.RegionSize;

        // Channel with bounded capacity for backpressure
        _chunkQueue = Channel.CreateBounded<(int, int)>(
            new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.DropOldest, // Or Wait, or DropNewest
            }
        );
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = new CancellationTokenSource();
        _api.Event.ChunkColumnLoaded += OnChunkColumnLoaded;
        _api.Event.ChunkDirty += OnChunkDirty;

        _syncSubscription = _messageBus
            .GetObservable()
            .Where(msg => msg is SyncMapCommand)
            .Subscribe(msg =>
            {
                var command = (SyncMapCommand)msg;

                // TODO: Tract server side mapchunk hashes
                var knownChunks = command.Data.KnownChunks;

                foreach (var chunk in knownChunks)
                {
                    UpdateChunkHash(chunk.ChunkX, chunk.ChunkZ, chunk.ContentHash);
                }
                _isReadyToSendMapChunks = true;
                _readyToSendMapChunksTcs.TrySetResult(true);
            });

        // Subscribe to PlayerJoinedEvent to send initial player position
        _playerJoinedSubscription = _messageBus
            .GetObservable()
            .Where(msg => msg is PlayerJoinedEvent)
            .Subscribe(msg =>
            {
                var joinEvent = (PlayerJoinedEvent)msg;
                SendPlayerPosition(joinEvent.Data.PlayerUID);
            });

        // Start background processor
        _processingTask = ProcessChunkQueueAsync(_cts.Token);
        _processingPlayerPositionsTask = ProcessPlayerPositionsAsync(_cts.Token);

        _logger.Notification("WorldMapHostedService started");
        return Task.CompletedTask;
    }

    private async Task ProcessPlayerPositionsAsync(CancellationToken cancellationToken)
    {
        var lastKnownPositions = new ConcurrentDictionary<string, Vec3f>();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _api.Server.Players.Foreach(player =>
                {
                    try
                    {
                        // Safety check - player entity might not be loaded
                        if (player?.Entity?.Pos == null)
                            return;

                        var playerPos = player.Entity.Pos.XYZFloat;
                        var playerUID = player.PlayerUID;

                        // Check if player has moved significantly
                        if (lastKnownPositions.TryGetValue(playerUID, out var lastPos))
                        {
                            var distance = Math.Sqrt(
                                Math.Pow(playerPos.X - lastPos.X, 2)
                                    + Math.Pow(playerPos.Y - lastPos.Y, 2)
                                    + Math.Pow(playerPos.Z - lastPos.Z, 2)
                            );

                            if (distance < _playerPositionMovementThreshold)
                                return; // Skip if player hasn't moved enough
                        }

                        // Update last known position
                        lastKnownPositions[playerUID] = playerPos;

                        var playerPositionChangedEvent =
                            _messageBus.CreateEvent<PlayerPositionChangedEvent>(evt =>
                            {
                                evt.Data.PlayerUID = playerUID;
                                evt.Data.X = playerPos.X;
                                evt.Data.Y = playerPos.Y;
                                evt.Data.Z = playerPos.Z;
                            });

                        _messageBus.Publish(playerPositionChangedEvent);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning(
                            $"Error processing position for player {player?.PlayerName}: {ex.Message}"
                        );
                    }
                });

                // Use await instead of blocking Wait()
                await Task.Delay(_playerPositionUpdateInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown, exit gracefully
                break;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in player position processing loop: {ex}");
                // Continue running despite errors
                await Task.Delay(_playerPositionUpdateInterval, cancellationToken);
            }
        }

        _logger.Debug("Player position processing stopped");
    }

    private void OnChunkDirty(Vec3i chunkCoord, IWorldChunk chunk, EnumChunkDirtyReason reason)
    {
        //throw new NotImplementedException();
    }

    private void OnChunkColumnLoaded(Vec2i chunkCoord, IWorldChunk[] chunks)
    {
        // Non-blocking write to channel
        _chunkQueue.Writer.TryWrite((chunkCoord.X, chunkCoord.Y));
    }

    public bool ShouldProcessChunk(int chunkX, int chunkZ, string newHash)
    {
        var key = (chunkX, chunkZ);

        // Check if hash has changed
        if (_chunkHashes.TryGetValue(key, out var existingHash))
        {
            return existingHash != newHash; // Only process if hash changed
        }

        return true; // New chunk, should process
    }

    public void UpdateChunkHash(int chunkX, int chunkZ, string hash)
    {
        var key = (chunkX, chunkZ);
        _chunkHashes[key] = hash; // Thread-safe add/update
    }

    private async Task ProcessChunkQueueAsync(CancellationToken cancellationToken)
    {
        await foreach (var (chunkX, chunkZ) in _chunkQueue.Reader.ReadAllAsync(cancellationToken))
        {
            if (!_isReadyToSendMapChunks)
            {
                try
                {
                    await _readyToSendMapChunksTcs.Task.WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
            try
            {
                var chunkHash = _mapDataExtractionService.GetChunkHash(chunkX, chunkZ);

                if (chunkHash != null && !ShouldProcessChunk(chunkX, chunkZ, chunkHash))
                {
                    _logger.Debug($"Skipped chunk ({chunkX}, {chunkZ}) - no changes detected");
                    continue;
                }

                var chunkData = await _mapDataExtractionService.ExtractChunkDataAsync(
                    chunkX,
                    chunkZ
                );

                if (chunkData != null)
                {
                    var eventData = new MapChunkDataEventData
                    {
                        ChunkX = chunkX,
                        ChunkZ = chunkZ,
                        ContentHash = chunkData.ContentHash,
                        RainHeightMap = chunkData.RainHeightMap.Select(i => (int)i).ToArray(),
                        SurfaceBlockIds = chunkData.SurfaceBlockIds,
                        AverageTemperature = chunkData.AverageTemperature,
                        AverageRainfall = chunkData.AverageRainfall,
                        ExtractedAt = DateTime.UtcNow,
                    };

                    var mapChunkEvent = _messageBus.CreateEvent<MapChunkDataEvent>(eventMessage =>
                    {
                        eventMessage.Data = eventData;
                    });

                    if (ShouldProcessChunk(chunkX, chunkZ, chunkData.ContentHash))
                    {
                        UpdateChunkHash(chunkX, chunkZ, chunkData.ContentHash);

                        _messageBus.Publish(mapChunkEvent);
                        _logger.Debug($"Published chunk data for ({chunkX}, {chunkZ})");
                    }
                    else
                    {
                        _logger.Debug($"Skipped chunk ({chunkX}, {chunkZ}) - no changes detected");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(
                    $"Error extracting/sending chunk data for ({chunkX}, {chunkZ}): {ex}"
                );
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _api.Event.ChunkColumnLoaded -= OnChunkColumnLoaded;

        // Signal completion and wait for processor to finish
        _chunkQueue.Writer.Complete();
        _cts?.Cancel();

        if (_processingTask != null)
        {
            await _processingTask;
        }

        _logger.Notification("WorldMapHostedService stopped");
    }

    public void Dispose()
    {
        _syncSubscription?.Dispose();
        _playerJoinedSubscription?.Dispose();
        _cts?.Dispose();
    }

    private void SendPlayerPosition(string playerUID)
    {
        try
        {
            var player = _api.World.PlayerByUid(playerUID);
            if (player?.Entity?.Pos == null)
            {
                _logger.Debug($"Player {playerUID} entity not loaded yet, cannot send position");
                return;
            }

            var playerPos = player.Entity.Pos.XYZFloat;
            var playerPositionChangedEvent = _messageBus.CreateEvent<PlayerPositionChangedEvent>(
                evt =>
                {
                    evt.Data.PlayerUID = playerUID;
                    evt.Data.X = playerPos.X;
                    evt.Data.Y = playerPos.Y;
                    evt.Data.Z = playerPos.Z;
                }
            );

            _messageBus.Publish(playerPositionChangedEvent);
            _logger.Debug($"Sent initial position for player {player.PlayerName}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to send initial position for player {playerUID}: {ex.Message}");
        }
    }
}
