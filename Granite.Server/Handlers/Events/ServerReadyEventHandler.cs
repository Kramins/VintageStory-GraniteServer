using Granite.Server.Services;
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
    private readonly ILogger<ServerReadyEventHandler> _logger;

    public ServerReadyEventHandler(
        PersistentMessageBusService messageBus,
        ILogger<ServerReadyEventHandler> logger
    )
    {
        _messageBus = messageBus;
        _logger = logger;
    }

    async Task IEventHandler<ServerReadyEvent>.Handle(ServerReadyEvent @event)
    {
        _logger.LogInformation(
            "Server {ServerId} announced ready at {StartedAt} with version {ServerVersion}",
            @event.OriginServerId,
            @event.Data.StartedAt,
            @event.Data.ServerVersion
        );

        // Issue sync server configuration command
        await IssueSyncServerConfigCommandAsync(@event.OriginServerId);

        // TODO: Add more sync commands as needed:
        // - Player data sync
        // - World data sync
        // - Mod list sync
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
}
