
using GraniteServer.Messaging.Commands;
using GraniteServer.Messaging.Events;
using GraniteServer.Messaging.Handlers.Commands;
using GraniteServer.Mod;
using GraniteServer.Services;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace GraniteServer.Mod.Handlers.Commands;

public class PlayerCommandHandlers
    : ICommandHandler<KickPlayerCommand>,
        ICommandHandler<BanPlayerCommand>,
        ICommandHandler<UnbanPlayerCommand>
{
    private ICoreServerAPI _api;
    private ServerCommandService _commandService;
    private MessageBusService _messageBus;
    private GraniteModConfig _config;

    private PlayerDataManager PlayerDataManager => (PlayerDataManager)_api.PlayerData;

    public PlayerCommandHandlers(
        ICoreServerAPI api,
        ServerCommandService commandService,
        MessageBusService messageBus,
        GraniteModConfig config
    )
    {
        _api = api;
        _commandService = commandService;
        _messageBus = messageBus;
        _config = config;
    }

    async Task ICommandHandler<KickPlayerCommand>.Handle(KickPlayerCommand command)
    {
        var player = _api
            .Server.Players.Where(p => p.PlayerUID == command.Data!.PlayerId)
            .FirstOrDefault();
        if (player != null)
        {
            try
            {
                // player.Disconnect(reason);
                var result = await _commandService.KickUserAsync(
                    player.PlayerName,
                    command.Data!.Reason
                );

                var kickedEvent = _messageBus.CreateEvent<PlayerKickedEvent>(
                    _config.ServerId,
                    e =>
                    {
                        e.Data!.PlayerUID = command.Data.PlayerId;
                        e.Data!.PlayerName = player.PlayerName;
                        e.Data!.Reason = command.Data.Reason;
                        e.Data!.IssuedBy = "System";
                    }
                );
                _messageBus.Publish(kickedEvent);
            }
            catch (Exception)
            {
                // Handle exception
            }
        }
    }

    async Task ICommandHandler<BanPlayerCommand>.Handle(BanPlayerCommand command)
    {
        var playerId = command.Data!.PlayerId;
        var playerName = command.Data.PlayerName;
        var reason = command.Data.Reason;
        var untilDate = command.Data.ExpirationDate;
        var issuedBy = command.Data.IssuedBy;

        var isBanned = PlayerDataManager.BannedPlayers.Any(bp => bp.PlayerUID == playerId);
        if (isBanned)
        {
            PlayerDataManager.BannedPlayers.RemoveAll(bp => bp.PlayerUID == playerId);
        }

        PlayerDataManager.BannedPlayers.Add(
            new PlayerEntry
            {
                PlayerUID = playerId,
                PlayerName = playerName,
                Reason = reason,
                IssuedByPlayerName = issuedBy,
                UntilDate = untilDate ?? DateTime.MaxValue,
            }
        );

        PlayerDataManager.bannedListDirty = true;

        var bannedEvent = _messageBus.CreateEvent<PlayerBannedEvent>(
            _config.ServerId,
            e =>
            {
                e.Data!.PlayerUID = playerId;
                e.Data!.PlayerName = playerName;
                e.Data!.Reason = reason;
                e.Data!.ExpirationDate = untilDate;
                e.Data!.IssuedBy = issuedBy;
            }
        );
        _messageBus.Publish(bannedEvent);
    }

    Task ICommandHandler.Handle(object command)
    {
        throw new NotImplementedException();
    }

    Task ICommandHandler<UnbanPlayerCommand>.Handle(UnbanPlayerCommand command)
    {
        var playerId = command.Data!.PlayerId;

        PlayerDataManager.BannedPlayers.RemoveAll(pe => pe.PlayerUID == playerId);
        PlayerDataManager.bannedListDirty = true;

        var unbannedEvent = _messageBus.CreateEvent<PlayerUnbannedEvent>(
            _config.ServerId,
            e => { e.Data!.PlayerUID = playerId; }
        );
        _messageBus.Publish(unbannedEvent);

        return Task.CompletedTask;
    }
}
