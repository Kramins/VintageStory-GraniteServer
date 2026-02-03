using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraniteServer.Messaging.Commands;
using GraniteServer.Messaging.Events;
using GraniteServer.Messaging.Handlers.Commands;
using GraniteServer.Services;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace GraniteServer.Mod.Handlers.Commands;

public class QueryPlayerInventoryCommandHandler : ICommandHandler<QueryPlayerInventoryCommand>
{
    private ICoreServerAPI _api;
    private ClientMessageBusService _messageBus;
    private GraniteModConfig _config;

    public QueryPlayerInventoryCommandHandler(
        ICoreServerAPI api,
        ClientMessageBusService messageBus,
        GraniteModConfig config
    )
    {
        _api = api;
        _messageBus = messageBus;
        _config = config;
    }

    public Task Handle(QueryPlayerInventoryCommand command)
    {
        var playerId = command.Data!.PlayerId;
        var player = _api.Server.Players.FirstOrDefault(p => p.PlayerUID == playerId);

        if (player == null)
        {
            return Task.CompletedTask;
        }

        var serverPlayer = player as IServerPlayer;
        if (serverPlayer?.Entity == null)
        {
            return Task.CompletedTask;
        }

        var inventories = new Dictionary<string, List<InventorySlotEventData>>();
        
        // TODO: Access player inventories correctly via Vintage Story API
        // This is a placeholder - need to investigate correct API usage
        
        var snapshotEvent = _messageBus.CreateEvent<PlayerInventorySnapshotEvent>(
            _config.ServerId,
            e =>
            {
                e.Data!.PlayerUID = playerId;
                e.Data!.PlayerName = player.PlayerName;
                e.Data!.Inventories = inventories;
            }
        );
        _messageBus.Publish(snapshotEvent);

        return Task.CompletedTask;
    }
}

public class UpdateInventorySlotCommandHandler : ICommandHandler<UpdateInventorySlotCommand>
{
    private ICoreServerAPI _api;
    private ClientMessageBusService _messageBus;
    private GraniteModConfig _config;

    public UpdateInventorySlotCommandHandler(
        ICoreServerAPI api,
        ClientMessageBusService messageBus,
        GraniteModConfig config
    )
    {
        _api = api;
        _messageBus = messageBus;
        _config = config;
    }

    public Task Handle(UpdateInventorySlotCommand command)
    {
        var playerId = command.Data!.PlayerId;
        var player = _api.Server.Players.FirstOrDefault(p => p.PlayerUID == playerId);

        if (player == null)
        {
            return Task.CompletedTask;
        }

        var serverPlayer = player as IServerPlayer;
        if (serverPlayer?.Entity == null)
        {
            return Task.CompletedTask;
        }

        // TODO: Implement inventory slot update via Vintage Story API
        // Need to investigate correct way to access and modify player inventories

        return Task.CompletedTask;
    }
}

public class RemoveInventorySlotCommandHandler : ICommandHandler<RemoveInventorySlotCommand>
{
    private ICoreServerAPI _api;
    private ClientMessageBusService _messageBus;
    private GraniteModConfig _config;

    public RemoveInventorySlotCommandHandler(
        ICoreServerAPI api,
        ClientMessageBusService messageBus,
        GraniteModConfig config
    )
    {
        _api = api;
        _messageBus = messageBus;
        _config = config;
    }

    public Task Handle(RemoveInventorySlotCommand command)
    {
        var playerId = command.Data!.PlayerId;
        var player = _api.Server.Players.FirstOrDefault(p => p.PlayerUID == playerId);

        if (player == null)
        {
            return Task.CompletedTask;
        }

        var serverPlayer = player as IServerPlayer;
        if (serverPlayer?.Entity == null)
        {
            return Task.CompletedTask;
        }

        // TODO: Implement inventory slot removal via Vintage Story API
        // Need to investigate correct way to access and modify player inventories

        return Task.CompletedTask;
    }
}
