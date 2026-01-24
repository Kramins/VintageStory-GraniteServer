using System;
using System.Linq;
using System.Threading.Tasks;
using Granite.Server.Services;
using GraniteServer.Data;
using GraniteServer.Data.Entities;
using GraniteServer.Messaging.Commands;
using GraniteServer.Messaging.Events;
using Microsoft.Extensions.Logging;

namespace GraniteServer.Messaging.Handlers.Events;

public class PlayerEventsHandler
    : IEventHandler<PlayerWhitelistedEvent>,
        IEventHandler<PlayerUnwhitelistedEvent>,
        IEventHandler<PlayerBannedEvent>,
        IEventHandler<PlayerUnbannedEvent>,
        IEventHandler<PlayerLeaveEvent>,
        IEventHandler<PlayerJoinedEvent>,
        IEventHandler<PlayerKickedEvent>
{
    private readonly GraniteDataContext _dataContext;
    private readonly PersistentMessageBusService _messageBus;
    private readonly ILogger<PlayerEventsHandler> _logger;

    public PlayerEventsHandler(
        GraniteDataContext dataContext,
        PersistentMessageBusService messageBus,
        ILogger<PlayerEventsHandler> logger
    )
    {
        _dataContext = dataContext;
        _messageBus = messageBus;
        _logger = logger;
    }

    Task IEventHandler<PlayerWhitelistedEvent>.Handle(PlayerWhitelistedEvent command)
    {
        var playerEventData = command.Data!;

        var playerEntity = GetPlayerEntity(command.OriginServerId, playerEventData.PlayerUID);

        if (playerEntity != null)
        {
            playerEntity.IsWhitelisted = true;
            _dataContext.Players.Update(playerEntity);
            _dataContext.SaveChanges();
        }

        return Task.CompletedTask;
    }

    Task IEventHandler<PlayerUnwhitelistedEvent>.Handle(PlayerUnwhitelistedEvent command)
    {
        var playerEventData = command.Data!;

        var playerEntity = GetPlayerEntity(command.OriginServerId, playerEventData.PlayerUID);

        if (playerEntity != null)
        {
            playerEntity.IsWhitelisted = false;
            playerEntity.WhitelistedReason = null;
            playerEntity.WhitelistedBy = null;
            _dataContext.Players.Update(playerEntity);
            _dataContext.SaveChanges();
        }

        return Task.CompletedTask;
    }

    Task IEventHandler<PlayerBannedEvent>.Handle(PlayerBannedEvent command)
    {
        var playerEventData = command.Data!;

        var playerEntity = GetPlayerEntity(command.OriginServerId, playerEventData.PlayerUID);

        if (playerEntity != null)
        {
            playerEntity.IsBanned = true;
            playerEntity.BanReason = playerEventData.Reason;
            playerEntity.BanBy = playerEventData.IssuedBy;
            playerEntity.BanUntil = playerEventData.UntilDate ?? playerEventData.ExpirationDate;
            _dataContext.Players.Update(playerEntity);
            _dataContext.SaveChanges();
        }

        return Task.CompletedTask;
    }

    Task IEventHandler<PlayerUnbannedEvent>.Handle(PlayerUnbannedEvent command)
    {
        var playerEventData = command.Data!;

        var playerEntity = GetPlayerEntity(command.OriginServerId, playerEventData.PlayerUID);

        if (playerEntity != null)
        {
            playerEntity.IsBanned = false;
            playerEntity.BanReason = null;
            playerEntity.BanBy = null;
            playerEntity.BanUntil = null;
            _dataContext.Players.Update(playerEntity);
            _dataContext.SaveChanges();
        }

        return Task.CompletedTask;
    }

    Task IEventHandler.Handle(object command)
    {
        throw new NotImplementedException();
    }

    Task IEventHandler<PlayerLeaveEvent>.Handle(PlayerLeaveEvent command)
    {
        var playerEventData = command.Data!;

        if (playerEventData.SessionId.HasValue)
        {
            var playerSessionEntity = _dataContext.PlayerSessions.FirstOrDefault(ps =>
                ps.Id == playerEventData.SessionId.Value
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

    async Task IEventHandler<PlayerJoinedEvent>.Handle(PlayerJoinedEvent command)
    {
        var playerEventData = command.Data!;
        var playerEntity = GetPlayerEntity(command.OriginServerId, playerEventData.PlayerUID);

        if (playerEntity == null)
        {
            playerEntity = new PlayerEntity()
            {
                Id = Guid.NewGuid(),
                PlayerUID = playerEventData.PlayerUID,
                ServerId = command.OriginServerId,
                Name = playerEventData.PlayerName,
                FirstJoinDate = DateTime.UtcNow,
                LastJoinDate = DateTime.UtcNow,
            };
            _dataContext.Players.Add(playerEntity);
            _dataContext.SaveChanges();
        }
        else
        {
            playerEntity.Name = playerEventData.PlayerName;
            playerEntity.LastJoinDate = DateTime.UtcNow;
            _dataContext.Players.Update(playerEntity);
        }

        if (playerEventData.SessionId.HasValue)
        {
            // Close any open sessions for this player/server before creating a new one
            var openSessions = _dataContext
                .PlayerSessions.Where(ps =>
                    ps.PlayerId == playerEntity.Id
                    && ps.ServerId == command.OriginServerId
                    && ps.LeaveDate == null
                )
                .ToList();

            foreach (var session in openSessions)
            {
                session.LeaveDate = DateTime.UtcNow;
                session.Duration = (session.LeaveDate - session.JoinDate)?.TotalSeconds;
                _dataContext.PlayerSessions.Update(session);
            }

            // Avoid duplicate insert if this session already exists
            var existing = _dataContext.PlayerSessions.FirstOrDefault(ps =>
                ps.Id == playerEventData.SessionId.Value
            );
            if (existing == null)
            {
                var playerSessionEntity = new PlayerSessionEntity()
                {
                    Id = playerEventData.SessionId.Value,
                    PlayerId = playerEntity.Id,
                    ServerId = command.OriginServerId,
                    JoinDate = DateTime.UtcNow,
                    IpAddress = playerEventData.IpAddress,
                    PlayerName = playerEventData.PlayerName,
                };

                _dataContext.PlayerSessions.Add(playerSessionEntity);
            }

            _dataContext.SaveChanges();
        }

        // Issue QueryPlayerInventoryCommand to sync player's inventory
        await IssueQueryPlayerInventoryCommandAsync(
            command.OriginServerId,
            playerEventData.PlayerUID,
            playerEventData.PlayerName
        );
    }

    Task IEventHandler<PlayerKickedEvent>.Handle(PlayerKickedEvent command)
    {
        throw new NotImplementedException();
    }

    private PlayerEntity? GetPlayerEntity(Guid serverId, string playerUID)
    {
        return _dataContext.Players.FirstOrDefault(p =>
            p.PlayerUID == playerUID && p.ServerId == serverId
        );
    }

    private async Task IssueQueryPlayerInventoryCommandAsync(
        Guid serverId,
        string playerUID,
        string playerName
    )
    {
        try
        {
            var queryCommand = _messageBus.CreateCommand<QueryPlayerInventoryCommand>(
                serverId,
                cmd =>
                {
                    cmd.Data.PlayerId = playerUID;
                    cmd.Data.PlayerName = playerName;
                }
            );
            await _messageBus.PublishCommandAsync(queryCommand);
         
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to issue QueryPlayerInventoryCommand for player {PlayerName} ({PlayerUID}) on server {ServerId}",
                playerName,
                playerUID,
                serverId
            );
        }
    }
}
