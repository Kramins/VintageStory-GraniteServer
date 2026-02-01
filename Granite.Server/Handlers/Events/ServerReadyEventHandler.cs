using Granite.Server.Services;
using Granite.Server.Services.Map;
using GraniteServer.Messaging.Commands;
using GraniteServer.Messaging.Events;
using GraniteServer.Messaging.Handlers.Events;
using GraniteServer.Services;
using Microsoft.Extensions.Logging;

namespace GraniteServer.Handlers.Events;

/// <summary>
/// Handles ServerReadyEvent from game server mods.
/// Initiates synchronization of server data when the server announces it's ready.
/// </summary>
public class ServerReadyEventHandler : IEventHandler<ServerReadyEvent>
{
    private readonly PersistentMessageBusService _messageBus;
    private readonly IMapDataStorageService _mapStorageService;
    private readonly ILogger<ServerReadyEventHandler> _logger;

    public ServerReadyEventHandler(
        PersistentMessageBusService messageBus,
        IMapDataStorageService mapStorageService,
        ILogger<ServerReadyEventHandler> logger
    )
    {
        _messageBus = messageBus;
        _mapStorageService = mapStorageService;
        _logger = logger;
    }

    async Task IEventHandler<ServerReadyEvent>.Handle(ServerReadyEvent @event)
    {
        try
        {
            // Issue sync server configuration command
            await IssueSyncServerConfigCommandAsync(@event.OriginServerId);

            // Issue sync collectibles command to populate collectibles cache
            await IssueSyncCollectiblesCommandAsync(@event.OriginServerId);

            // Issue sync map command to synchronize map chunk data (one-time on server startup)
            await IssueSyncMapCommandAsync(@event.OriginServerId);

            // TODO: Add more sync commands as needed:
            // - Player data sync
            // - World data sync
            // - Mod list sync
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error during server ready event handling for server {ServerId}",
                @event.OriginServerId
            );
        }
    }

    private async Task IssueSyncServerConfigCommandAsync(Guid serverId)
    {
        try
        {
            var syncCommand = _messageBus.CreateCommand<SyncServerConfigCommand>(
                serverId,
                cmd => { }
            );
            await _messageBus.PublishCommandAsync(syncCommand);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to issue SyncServerConfigCommand to server {ServerId}",
                serverId
            );
        }
    }

    private async Task IssueSyncCollectiblesCommandAsync(Guid serverId)
    {
        try
        {
            var syncCommand = _messageBus.CreateCommand<SyncCollectiblesCommand>(
                serverId,
                cmd => { }
            );
            await _messageBus.PublishCommandAsync(syncCommand);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to issue SyncCollectiblesCommand to server {ServerId}",
                serverId
            );
        }
    }

    private async Task IssueSyncMapCommandAsync(Guid serverId)
    {
        try
        {
            // Get all known chunks with hashes from database
            var knownChunks = await _mapStorageService.GetAllChunkHashesAsync(serverId);

            var syncCommand = _messageBus.CreateCommand<SyncMapCommand>(
                serverId,
                cmd =>
                {
                    cmd.Data.KnownChunks = knownChunks
                        .Select(h => new ChunkHashInfo
                        {
                            ChunkX = h.ChunkX,
                            ChunkZ = h.ChunkZ,
                            ContentHash = h.ContentHash,
                        })
                        .ToList();
                }
            );
            await _messageBus.PublishCommandAsync(syncCommand);

            _logger.LogInformation(
                "Issued SyncMapCommand to server {ServerId} with {ChunkCount} known chunks",
                serverId,
                knownChunks.Count
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to issue SyncMapCommand to server {ServerId}",
                serverId
            );
        }
    }
}
