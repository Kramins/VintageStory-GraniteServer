using System;
using System.Collections.Generic;
using System.Linq;
using Granite.Common.Dto;
using GraniteServer.Data;
using GraniteServer.Messaging.Commands;
using GraniteServer.Services;
using Microsoft.EntityFrameworkCore;

namespace Granite.Server.Services;

public class ServerPlayersService
{
    private MessageBusService _messageBus;
    private GraniteDataContext _dbContext;

    public ServerPlayersService(MessageBusService messageBus, GraniteDataContext dbContext)
    {
        _messageBus = messageBus;
        _dbContext = dbContext;
    }

    public async Task<List<PlayerDTO>> GetPlayersAsync(Guid serverId)
    {
        var serverPlayers =
            from player in _dbContext.Players
            where player.ServerId == serverId
            let isPlaying = player.Sessions.Any(s => s.LeaveDate == null)
            select new PlayerDTO
            {
                Id = player.Id,
                ServerId = player.ServerId,
                PlayerUID = player.PlayerUID,
                Name = player.Name,
                FirstJoinDate = player.FirstJoinDate.ToString("o"),
                LastJoinDate = player.LastJoinDate.ToString("o"),
                IsWhitelisted = player.IsWhitelisted,
                WhitelistedBy = player.WhitelistedBy,
                WhitelistedReason = player.WhitelistedReason,
                IsBanned = player.IsBanned,
                BanReason = player.BanReason,
                BanBy = player.BanBy,
                BanUntil = player.BanUntil,
                ConnectionState = isPlaying ? "Connected" : "Disconnected",
            };

        return await serverPlayers.ToListAsync();
    }

    public async Task<PlayerDetailsDTO?> GetPlayerDetailsAsync(Guid serverId, Guid playerId)
    {
        var player = await _dbContext
            .Players
            .Include(p => p.Sessions)
            .FirstOrDefaultAsync(p => p.ServerId == serverId && p.Id == playerId);

        if (player == null)
        {
            return null;
        }

        var isPlaying = player.Sessions.Any(s => s.LeaveDate == null);
        var lastSession = player.Sessions
            .OrderByDescending(s => s.JoinDate)
            .FirstOrDefault();

        return new PlayerDetailsDTO
        {
            Id = player.Id,
            ServerId = player.ServerId,
            PlayerUID = player.PlayerUID,
            Name = player.Name,
            FirstJoinDate = player.FirstJoinDate.ToString("o"),
            LastJoinDate = player.LastJoinDate.ToString("o"),
            IsWhitelisted = player.IsWhitelisted,
            WhitelistedBy = player.WhitelistedBy,
            WhitelistedReason = player.WhitelistedReason,
            IsBanned = player.IsBanned,
            BanReason = player.BanReason,
            BanBy = player.BanBy,
            BanUntil = player.BanUntil,
            ConnectionState = isPlaying ? "Connected" : "Disconnected",
            IpAddress = lastSession?.IpAddress ?? string.Empty,
            IsAdmin = false,
            LanguageCode = string.Empty,
            Ping = 0,
            RolesCode = string.Empty,
            Privileges = Array.Empty<string>(),
            WhitelistedUntil = null,
            Inventories = new Dictionary<string, InventoryDTO>()
        };
    }

    public void WhitelistPlayer(Guid serverId, string playerId, string? reason)
    {
        var whitelistPlayerCommand = _messageBus.CreateCommand<WhitelistPlayerCommand>(
            serverId,
            cmd =>
            {
                cmd.Data.PlayerId = playerId;
                cmd.Data.Reason = reason;
            }
        );
    }

    public void UnwhitelistPlayer(Guid serverId, string playerId)
    {
        var unwhitelistPlayerCommand = _messageBus.CreateCommand<UnwhitelistPlayerCommand>(
            serverId,
            cmd =>
            {
                cmd.Data.PlayerId = playerId;
            }
        );
    }

    public void BanPlayer(
        Guid serverId,
        string playerId,
        string reason,
        DateTime? expirationDate,
        string? issuedBy
    )
    {
        var banPlayerCommand = _messageBus.CreateCommand<BanPlayerCommand>(
            serverId,
            cmd =>
            {
                cmd.Data.PlayerId = playerId;
                cmd.Data.Reason = reason;
                cmd.Data.ExpirationDate = expirationDate;
                cmd.Data.IssuedBy = issuedBy ?? string.Empty;
                cmd.Data.PlayerName = string.Empty;
            }
        );
    }

    public void UnbanPlayer(Guid serverId, string playerId)
    {
        var unbanPlayerCommand = _messageBus.CreateCommand<UnbanPlayerCommand>(
            serverId,
            cmd =>
            {
                cmd.Data.PlayerId = playerId;
            }
        );
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
