using System;
using System.Linq;
using System.Threading.Tasks;
using GraniteServer.Data;
using GraniteServer.Data.Entities;
using GraniteServer.Messaging.Events;

namespace GraniteServer.Messaging.Handlers.Events;

public class PlayerEventsHandler
    : IEventHandler<PlayerWhitelistedEvent>,
        IEventHandler<PlayerUnwhitelistedEvent>,
        IEventHandler<PlayerLeaveEvent>,
        IEventHandler<PlayerJoinedEvent>,
        IEventHandler<PlayerKickedEvent>
{
    private GraniteDataContext _dataContext;

    public PlayerEventsHandler(GraniteDataContext dataContext)
    {
        _dataContext = dataContext;
    }

    Task IEventHandler<PlayerWhitelistedEvent>.Handle(PlayerWhitelistedEvent command)
    {
        throw new NotImplementedException();
    }

    Task IEventHandler<PlayerUnwhitelistedEvent>.Handle(PlayerUnwhitelistedEvent command)
    {
        throw new NotImplementedException();
    }

    Task IEventHandler.Handle(object command)
    {
        throw new NotImplementedException();
    }

    Task IEventHandler<PlayerLeaveEvent>.Handle(PlayerLeaveEvent command)
    {
        var playerEventData = command.Data!;

        var sessionIdStr = playerEventData.SessionId;
        if (Guid.TryParse(sessionIdStr, out var sessionGuid))
        {
            var playerSessionEntity = _dataContext.PlayerSessions.FirstOrDefault(ps =>
                ps.Id == sessionGuid
            );
            if (playerSessionEntity != null)
            {
                playerSessionEntity.LeaveDate = DateTime.UtcNow;
                playerSessionEntity.Duration = (
                    playerSessionEntity.LeaveDate - playerSessionEntity.JoinDate
                )?.TotalSeconds;
                _dataContext.PlayerSessions.Update(playerSessionEntity);
                _dataContext.SaveChanges();
            }
        }

        return Task.CompletedTask;
    }

    Task IEventHandler<PlayerJoinedEvent>.Handle(PlayerJoinedEvent command)
    {
        var playerEventData = command.Data!;
        var playerEntity = _dataContext.Players.FirstOrDefault(p =>
            p.Id == playerEventData.PlayerId && p.ServerId == command.OriginServerId
        );
        if (playerEntity == null)
        {
            playerEntity = new PlayerEntity()
            {
                Id = playerEventData.PlayerId,
                ServerId = command.OriginServerId,
                Name = playerEventData.PlayerName,
                FirstJoinDate = DateTime.UtcNow,
                LastJoinDate = DateTime.UtcNow,
            };
            _dataContext.Players.Add(playerEntity);
        }
        else
        {
            playerEntity.Name = playerEventData.PlayerName;
            playerEntity.LastJoinDate = DateTime.UtcNow;
            _dataContext.Players.Update(playerEntity);
        }

        var playerSessionId = Guid.Parse(playerEventData.SessionId);

        var playerSessionEntity = new PlayerSessionEntity()
        {
            Id = playerSessionId,
            PlayerId = playerEventData.PlayerId,
            ServerId = command.OriginServerId,
            JoinDate = DateTime.UtcNow,
            IpAddress = playerEventData.IpAddress,
            PlayerName = playerEventData.PlayerName,
        };

        _dataContext.PlayerSessions.Add(playerSessionEntity);
        _dataContext.SaveChanges();

        return Task.CompletedTask;
    }

    Task IEventHandler<PlayerKickedEvent>.Handle(PlayerKickedEvent command)
    {
        throw new NotImplementedException();
    }
}
