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
    private readonly IServerWorldMapService _mapService;
    private readonly ServerConfigService _configService;
    private readonly ServerPlayersService _playerService;
    private readonly ILogger<ServerReadyEventHandler> _logger;

    public ServerReadyEventHandler(
        PersistentMessageBusService messageBus,
        IServerWorldMapService mapService,
        ServerConfigService configService,
        ServerPlayersService playerService,
        ILogger<ServerReadyEventHandler> logger
    )
    {
        _messageBus = messageBus;
        _mapService = mapService;
        _configService = configService;
        _playerService = playerService;
        _logger = logger;
    }

    async Task IEventHandler<ServerReadyEvent>.Handle(ServerReadyEvent @event)
    {
        try
        {
            // Push server configuration from database to game server on startup
            await PushServerConfigToGameServerAsync(@event.OriginServerId);

            // Issue sync collectibles command to populate collectibles cache
            await IssueSyncCollectiblesCommandAsync(@event.OriginServerId);

            // Issue sync map command to synchronize map chunk data (one-time on server startup)
            await IssueSyncMapCommandAsync(@event.OriginServerId);

            // Sync player moderation data (bans and whitelists) from database to game server
            await IssueSyncPlayerModerationDataCommandAsync(@event.OriginServerId);

            // TODO: Add more sync commands as needed:
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

    private async Task PushServerConfigToGameServerAsync(Guid serverId)
    {
        try
        {
            // Fetch config from database and push to game server
            var config = await _configService.GetServerConfigAsync(serverId);
            if (config == null)
            {
                _logger.LogWarning(
                    "Cannot push config to server {ServerId} - config not found in database",
                    serverId
                );
                return;
            }

            var updateCommand = _messageBus.CreateCommand<UpdateServerConfigCommand>(
                serverId,
                cmd =>
                {
                    cmd.Data.Config = config;
                }
            );
            await _messageBus.PublishCommandAsync(updateCommand);
            _logger.LogInformation(
                "Pushed configuration from database to game server {ServerId} on startup",
                serverId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to push server config to server {ServerId} on startup",
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
            var knownChunks = await _mapService.GetAllChunkHashesAsync(serverId);

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
            _logger.LogError(ex, "Failed to issue SyncMapCommand to server {ServerId}", serverId);
        }
    }

    private async Task IssueSyncPlayerModerationDataCommandAsync(Guid serverId)
    {
        try
        {
            // Get all players from database via service
            var allPlayers = await _playerService.GetPlayersAsync(serverId);

            // Filter to only banned/whitelisted players and exclude expired bans
            var players = allPlayers
                .Where(p => p.IsBanned || p.IsWhitelisted)
                .Where(p => !p.IsBanned || p.BanUntil == null || p.BanUntil > DateTime.UtcNow)
                .Select(p => new PlayerModerationRecord
                {
                    PlayerUID = p.PlayerUID,
                    Name = p.Name,
                    IsBanned = p.IsBanned,
                    BanReason = p.BanReason,
                    BanBy = p.BanBy,
                    BanUntil = p.BanUntil,
                    IsWhitelisted = p.IsWhitelisted,
                    WhitelistedReason = p.WhitelistedReason,
                    WhitelistedBy = p.WhitelistedBy,
                })
                .ToList();

            var syncCommand = _messageBus.CreateCommand<SyncPlayerModerationDataCommand>(
                serverId,
                cmd => { cmd.Data.Players = players; }
            );

            await _messageBus.PublishCommandAsync(syncCommand);

            _logger.LogInformation(
                "Issued SyncPlayerModerationDataCommand to server {ServerId} with {PlayerCount} player moderation records",
                serverId,
                players.Count
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to issue SyncPlayerModerationDataCommand to server {ServerId}",
                serverId
            );
        }
    }
}
