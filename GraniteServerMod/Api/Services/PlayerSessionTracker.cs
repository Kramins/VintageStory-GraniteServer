using System;
using GraniteServerMod.Data;
using GraniteServerMod.Data.Entities;
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

    private readonly ILogger _logger;

    public PlayerSessionTracker(
        GraniteDataContext dataContext,
        GraniteServerConfig config,
        ICoreServerAPI api,
        ILogger logger
    )
    {
        _dataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        var playerEntity = _dataContext.Players.Find(byPlayer.PlayerUID);
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
                var playerSessionEntity = _dataContext.PlayerSessions.Find(sessionGuid);
                if (playerSessionEntity != null)
                {
                    playerSessionEntity.LeaveDate = DateTime.UtcNow;
                    _dataContext.PlayerSessions.Update(playerSessionEntity);
                    _dataContext.SaveChanges();
                }
            }

            byPlayer.ServerData.CustomPlayerData.Remove("GraniteSessionId");
        }
    }
}
