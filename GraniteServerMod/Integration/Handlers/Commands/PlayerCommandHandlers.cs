using System;
using System.Linq;
using System.Threading.Tasks;
using GraniteServer.Api.Services;
using GraniteServer.Messaging.Commands;
using GraniteServer.Messaging.Events;
using GraniteServer.Messaging.Handlers.Commands;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace GraniteServer.Integration.Handlers.Commands;

public class PlayerCommandHandlers
    : ICommandHandler<KickPlayerCommand>,
        ICommandHandler<BanPlayerCommand>,
        ICommandHandler<UnbanPlayerCommand>
{
    private ICoreServerAPI _api;
    private ServerCommandService _commandService;
    private MessageBusService _messageBus;

    private PlayerDataManager PlayerDataManager => (PlayerDataManager)_api.PlayerData;

    public PlayerCommandHandlers(
        ICoreServerAPI api,
        ServerCommandService commandService,
        MessageBusService messageBus
    )
    {
        _api = api;
        _commandService = commandService;
        _messageBus = messageBus;
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

                _messageBus.Publish(
                    new PlayerKickedEvent()
                    {
                        Data = new PlayerKickedEventData
                        {
                            PlayerId = command.Data.PlayerId,
                            PlayerName = player.PlayerName,
                            Reason = command.Data.Reason,
                            IssuedBy = "System",
                        },
                    }
                );
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

        _messageBus.Publish(
            new PlayerBannedEvent()
            {
                Data = new PlayerBannedEventData
                {
                    PlayerId = playerId,
                    PlayerName = playerName,
                    Reason = reason,
                    ExpirationDate = untilDate,
                    IssuedBy = issuedBy,
                },
            }
        );
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

        _messageBus.Publish(
            new PlayerUnbannedEvent() { Data = new PlayerUnbannedEventData { PlayerId = playerId } }
        );

        return Task.CompletedTask;
    }
}
