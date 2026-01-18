using System;
using GraniteServer.Messaging.Commands;
using GraniteServer.Services;

namespace Granite.Server.Services;

public class ServerPlayersService
{
    private MessageBusService _messageBus;

    public ServerPlayersService(MessageBusService messageBus)
    {
        _messageBus = messageBus;
    }

    public void KickPlayer(Guid serverId, string playerId, string reason)
    {
        var kickPlayerCommand = _messageBus.CreateCommand<KickPlayerCommand>(
            serverId,
            cmd =>
            {
                cmd.Data.PlayerId = playerId;
                cmd.Data.Reason = reason;
            }
        );
    }
}
