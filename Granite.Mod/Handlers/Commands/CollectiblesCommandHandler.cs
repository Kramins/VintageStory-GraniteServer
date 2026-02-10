using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraniteServer.Messaging.Commands;
using GraniteServer.Messaging.Events;
using GraniteServer.Messaging.Handlers.Commands;
using GraniteServer.Services;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace GraniteServer.Mod.Handlers.Commands;

public class SyncCollectiblesCommandHandler : ICommandHandler<SyncCollectiblesCommand>
{
    private ICoreServerAPI _api;
    private ClientMessageBusService _messageBus;
    private GraniteModConfig _config;

    public SyncCollectiblesCommandHandler(
        ICoreServerAPI api,
        ClientMessageBusService messageBus,
        GraniteModConfig config
    )
    {
        _api = api;
        _messageBus = messageBus;
        _config = config;
    }

    public Task Handle(SyncCollectiblesCommand command)
    {
        var collectibles = _api.World.Collectibles
            .Select(c => MapCollectibleToEventData(c))
            .ToList();

        var collectiblesEvent = _messageBus.CreateEvent<CollectiblesLoadedEvent>(
            _config.ServerId,
            e =>
            {
                e.Data!.Collectibles = collectibles;
            }
        );
        _messageBus.Publish(collectiblesEvent);

        return Task.CompletedTask;
    }

    private CollectibleEventData MapCollectibleToEventData(CollectibleObject collectible)
    {
        var text = collectible.ItemClass.Name();
        
        var itemName = Lang.GetMatching(
            collectible.Code?.Domain + ":" + text + "-" + collectible.Code?.Path,
            new[] { "" }
        );

        var type = "item";
        var blockMaterial = "";
        var mapColorCode = "";
        if (collectible is Block)
        {
            var block = (Block)collectible;
            blockMaterial = block.BlockMaterial.ToString();
            type = "block";
            mapColorCode = block.Attributes?["mapColorCode"]?.AsString() ?? "";
        }

        return new CollectibleEventData
        {
            Id = collectible.Id,
            Domain = collectible.Code?.Domain ?? "",
            Path = collectible.Code?.Path ?? "",
            Name = itemName,
            Class = collectible.Class,
            BlockMaterial = blockMaterial,
            MaxStackSize = collectible.MaxStackSize,
            Type = type,
            MapColorCode = mapColorCode
        };
    }
}
