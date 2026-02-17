using GraniteServer.Messaging.Commands;
using GraniteServer.Messaging.Events;
using GraniteServer.Mod;
using GraniteServer.Services;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace GraniteServer.HostedServices;

/// <summary>
/// Hosted service that handles collectibles synchronization commands.
/// Subscribes directly to the message bus for collectibles-related commands.
/// </summary>
public class CollectiblesHostedService : GraniteHostedServiceBase
{
    private readonly ICoreServerAPI _api;
    private readonly GraniteModConfig _config;

    public CollectiblesHostedService(
        ICoreServerAPI api,
        ClientMessageBusService messageBus,
        GraniteModConfig config,
        ILogger logger
    )
        : base(messageBus, logger)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        LogNotification("Starting service...");

        SubscribeToCommand<SyncCollectiblesCommand>(HandleSyncCollectiblesCommand);

        LogNotification("Service started");
        return Task.CompletedTask;
    }

    private void HandleSyncCollectiblesCommand(SyncCollectiblesCommand command)
    {
        LogNotification("Syncing collectibles from game world");

        var collectibles = _api
            .World.Collectibles.Select(c => MapCollectibleToEventData(c))
            .ToList();

        LogNotification("Found {collectibles.Count} collectibles");

        var collectiblesEvent = MessageBus.CreateEvent<CollectiblesLoadedEvent>(
            _config.ServerId,
            e =>
            {
                e.Data!.Collectibles = collectibles;
            }
        );
        MessageBus.Publish(collectiblesEvent);

        LogNotification("Collectibles synced successfully");
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
            MapColorCode = mapColorCode,
        };
    }
}
