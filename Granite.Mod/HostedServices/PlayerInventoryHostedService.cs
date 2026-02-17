using GraniteServer.Messaging.Commands;
using GraniteServer.Messaging.Events;
using GraniteServer.Mod;
using GraniteServer.Services;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace GraniteServer.HostedServices;

/// <summary>
/// Hosted service that handles player inventory commands for querying and modifying player inventories.
/// Subscribes directly to the message bus for player inventory commands.
/// </summary>
public class PlayerInventoryHostedService : GraniteHostedServiceBase
{
    private readonly ICoreServerAPI _api;
    private readonly GraniteModConfig _config;

    public PlayerInventoryHostedService(
        ICoreServerAPI api,
        ClientMessageBusService messageBus,
        GraniteModConfig config,
        ILogger logger
    ) : base(messageBus, logger)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        LogNotification("Starting service...");

        SubscribeToCommand<QueryPlayerInventoryCommand>(HandleQueryPlayerInventoryCommand);
        SubscribeToCommand<UpdateInventorySlotCommand>(HandleUpdateInventorySlotCommand);
        SubscribeToCommand<RemoveInventorySlotCommand>(HandleRemoveInventorySlotCommand);

        LogNotification("Service started");
        return Task.CompletedTask;
    }

    private void HandleQueryPlayerInventoryCommand(QueryPlayerInventoryCommand command)
    {
        var playerId = command.Data!.PlayerId;
        var player = _api.Server.Players.FirstOrDefault(p => p.PlayerUID == playerId);

        if (player == null)
        {
            LogWarning("Player with UID {playerId} not found for inventory query");
            return;
        }

        var serverPlayer = player as IServerPlayer;
        if (serverPlayer?.Entity == null)
        {
            LogWarning("Player {player.PlayerName} has no entity for inventory query");
            return;
        }

        var inventories = new Dictionary<string, List<InventorySlotEventData>>();
        
        // TODO: Access player inventories correctly via Vintage Story API
        // This is a placeholder - need to investigate correct API usage
        
        var snapshotEvent = MessageBus.CreateEvent<PlayerInventorySnapshotEvent>(
            _config.ServerId,
            e =>
            {
                e.Data!.PlayerUID = playerId;
                e.Data!.PlayerName = player.PlayerName;
                e.Data!.Inventories = inventories;
            }
        );
        MessageBus.Publish(snapshotEvent);

        LogNotification("Published inventory snapshot for player {player.PlayerName}");
    }

    private void HandleUpdateInventorySlotCommand(UpdateInventorySlotCommand command)
    {
        var playerId = command.Data!.PlayerId;
        var player = _api.Server.Players.FirstOrDefault(p => p.PlayerUID == playerId);

        if (player == null)
        {
            LogWarning("Player with UID {playerId} not found for inventory update");
            return;
        }

        var serverPlayer = player as IServerPlayer;
        if (serverPlayer?.Entity == null)
        {
            LogWarning("Player {player.PlayerName} has no entity for inventory update");
            return;
        }

        // TODO: Implement inventory slot update via Vintage Story API
        // Need to investigate correct way to access and modify player inventories

        LogNotification("Inventory update command received for player {player.PlayerName} (not yet implemented)");
    }

    private void HandleRemoveInventorySlotCommand(RemoveInventorySlotCommand command)
    {
        var playerId = command.Data!.PlayerId;
        var player = _api.Server.Players.FirstOrDefault(p => p.PlayerUID == playerId);

        if (player == null)
        {
            LogWarning("Player with UID {playerId} not found for inventory removal");
            return;
        }

        var serverPlayer = player as IServerPlayer;
        if (serverPlayer?.Entity == null)
        {
            LogWarning("Player {player.PlayerName} has no entity for inventory removal");
            return;
        }

        // TODO: Implement inventory slot removal via Vintage Story API
        // Need to investigate correct way to access and modify player inventories

        LogNotification("Inventory removal command received for player {player.PlayerName} (not yet implemented)");
    }
}
