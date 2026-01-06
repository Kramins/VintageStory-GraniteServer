using System;
using System.Linq;
using GraniteServer.Api.Messaging.Events;
using GraniteServer.Api.Messaging.Contracts;
using GraniteServer.Data;
using GraniteServer.Data.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace GraniteServer.Api.Services;

/// <summary>
/// Tracks player sessions and updates player records in the database.
/// Handles game lifecycle events for player join and leave.
/// </summary>
public class PlayerSessionTracker
{
    private readonly GraniteDataContext _dataContext;
    private readonly GraniteServerConfig _config;
    private readonly ICoreServerAPI _api;
    private readonly MessageBusService _messageBus;
    private readonly ILogger _logger;

    public PlayerSessionTracker(
        GraniteDataContext dataContext,
        GraniteServerConfig config,
        ICoreServerAPI api,
        MessageBusService messageBus,
        ILogger logger
    )
    {
        _dataContext = dataContext;
        _config = config;
        _api = api;
        _messageBus = messageBus;
        _logger = logger;
    }

    /// <summary>
    /// Handles player join events by creating or updating player records and starting a new session.
    /// </summary>
    /// <param name="byPlayer">The player who joined the server.</param>
    public void OnPlayerJoin(IServerPlayer byPlayer)
    {
        _logger.Notification(
            $"[PlayerSessionTracker] Player joined: {byPlayer.PlayerName} ({byPlayer.PlayerUID})"
        );

        var playerEntity = _dataContext.Players.FirstOrDefault(p =>
            p.Id == byPlayer.PlayerUID && p.ServerId == _config.ServerId
        );
        if (playerEntity == null)
        {
            playerEntity = new PlayerEntity()
            {
                Id = byPlayer.PlayerUID,
                ServerId = _config.ServerId,
                Name = byPlayer.PlayerName,
                FirstJoinDate = DateTime.UtcNow,
                LastJoinDate = DateTime.UtcNow,
            };
            _dataContext.Players.Add(playerEntity);
        }
        else
        {
            playerEntity.Name = byPlayer.PlayerName;
            playerEntity.LastJoinDate = DateTime.UtcNow;
            _dataContext.Players.Update(playerEntity);
        }

        var playerSessionId = Guid.NewGuid();
        byPlayer.ServerData.CustomPlayerData["GraniteSessionId"] = playerSessionId.ToString();

        var playerSessionEntity = new PlayerSessionEntity()
        {
            Id = playerSessionId,
            PlayerId = byPlayer.PlayerUID,
            ServerId = _config.ServerId,
            JoinDate = DateTime.UtcNow,
            IpAddress = byPlayer.IpAddress,
            PlayerName = byPlayer.PlayerName,
        };

        _dataContext.PlayerSessions.Add(playerSessionEntity);
        _dataContext.SaveChanges();

        _messageBus.Publish(
            new PlayerJoinEvent()
            {
                Data = new()
                {
                    PlayerName = byPlayer.PlayerName,
                    PlayerId = byPlayer.PlayerUID,
                    SessionId = playerSessionId.ToString(),
                    TimeStamp = DateTime.UtcNow,
                },
            }
        );
    }

    /// <summary>
    /// Handles player leave events by updating the session end time.
    /// </summary>
    /// <param name="byPlayer">The player who left the server.</param>
    public void OnPlayerLeave(IServerPlayer byPlayer)
    {
        _logger.Notification(
            $"[PlayerSessionTracker] Player left: {byPlayer.PlayerName} ({byPlayer.PlayerUID})"
        );

        if (
            byPlayer.ServerData.CustomPlayerData.TryGetValue(
                "GraniteSessionId",
                out var sessionIdObj
            )
        )
        {
            var sessionIdStr = sessionIdObj?.ToString();
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

            byPlayer.ServerData.CustomPlayerData.Remove("GraniteSessionId");

            _messageBus.Publish(
                new PlayerLeaveEvent()
                {
                    Data = new()
                    {
                        PlayerName = byPlayer.PlayerName,
                        PlayerId = byPlayer.PlayerUID,
                        SessionId = sessionIdStr,
                        TimeStamp = DateTime.UtcNow,
                    },
                }
            );
        }
    }
}
