using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Threading.Channels;
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
    private Task _processingTask;
    private bool _isReadyToSendMapChunks;
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

        // Start background processor
        _processingTask = ProcessChunkQueueAsync(_cts.Token);

        _logger.Notification("WorldMapHostedService started");
        return Task.CompletedTask;
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
        _cts?.Dispose();
    }
}
