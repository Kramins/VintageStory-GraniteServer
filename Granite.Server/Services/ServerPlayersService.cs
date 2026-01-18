using System;
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
        var query =
            from p in _dbContext.Players
            join ps in _dbContext.PlayerSessions on new { PlayerId = p.Id, p.ServerId } equals new { ps.PlayerId, ps.ServerId } into sessions
            from latestSession in sessions.OrderByDescending(s => s.JoinDate).Take(1).DefaultIfEmpty()
            where p.ServerId == serverId
            select new PlayerDTO
            {
                Id = p.Id,
                Name = p.Name,
                FirstJoinDate = p.FirstJoinDate.ToString("o"),
                LastJoinDate = p.LastJoinDate.ToString("o"),
                // Runtime/state fields - will be populated later from other sources
                IsAdmin = false,
                IpAddress = latestSession != null ? latestSession.IpAddress : string.Empty,
                LanguageCode = string.Empty,
                Ping = 0,
                RolesCode = string.Empty,
                Privileges = Array.Empty<string>(),
                ConnectionState = latestSession != null && !latestSession.LeaveDate.HasValue ? "Playing" : "Offline",
                IsBanned = p.IsBanned,
                IsWhitelisted = p.IsWhitelisted,
                BanReason = p.BanReason,
                BanBy = p.BanBy,
                BanUntil = p.BanUntil,
                WhitelistedReason = p.WhitelistedReason,
                WhitelistedBy = p.WhitelistedBy,
            };

        return await query.ToListAsync();
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

    public void BanPlayer(Guid serverId, string playerId, string reason, DateTime? expirationDate, string? issuedBy)
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
