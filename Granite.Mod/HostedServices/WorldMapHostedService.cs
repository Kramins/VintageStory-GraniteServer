using System;
using Granite.Mod.Services.Map;
using GraniteServer.Mod;
using GraniteServer.Services;
using Microsoft.Extensions.Hosting;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Granite.Mod.HostedServices;

public class WorldMapHostedService : IHostedService, IDisposable
{
    private ICoreServerAPI _api;
    private readonly IMapDataExtractionService _mapDataExtractionService;
    private ClientMessageBusService _messageBus;
    private GraniteModConfig _config;
    private readonly ILogger _logger;

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
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _api.Event.ChunkColumnLoaded += OnChunkColumnLoaded;
        return Task.CompletedTask;
    }

    private void OnChunkColumnLoaded(Vec2i chunkCoord, IWorldChunk[] chunks)
    {
        //_mapDataExtractionService.ExtractChunkDataAsync(chunkCoord.X, chunkCoord.Y);
        _logger.Notification($"Chunk column loaded at {chunkCoord.X}, {chunkCoord.Y}");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
