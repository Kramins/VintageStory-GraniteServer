using System;
using System.Linq;
using System.Threading.Tasks;
using GraniteServer.Api.Services;
using GraniteServer.Messaging.Commands;
using GraniteServer.Messaging.Events;
using GraniteServer.Messaging.Handlers.Commands;
using Vintagestory.API.Server;

namespace GraniteServer.Integration.Handlers.Commands;

public class KickPlayerCommandHandler : ICommandHandler<KickPlayerCommand>
{
    private ICoreServerAPI _api;
    private ServerCommandService _commandService;
    private MessageBusService _messageBus;

    public KickPlayerCommandHandler(
        ICoreServerAPI api,
        ServerCommandService commandService,
        MessageBusService messageBus
    )
    {
        _api = api;
        _commandService = commandService;
        _messageBus = messageBus;
    }

    public async Task Handle(KickPlayerCommand command)
    {
        var player = _api
            .Server.Players.Where(p => p.PlayerUID == command.Data.PlayerId)
            .FirstOrDefault();
        if (player != null)
        {
            try
            {
                // player.Disconnect(reason);
                var result = await _commandService.KickUserAsync(
                    player.PlayerName,
                    command.Data.Reason
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
}
