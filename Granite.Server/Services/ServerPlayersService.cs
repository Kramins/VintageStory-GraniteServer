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
    private readonly ILogger<ServerPlayersService> _logger;
    private PersistentMessageBusService _messageBus;
    private GraniteDataContext _dbContext;

    public ServerPlayersService(
        ILogger<ServerPlayersService> logger,
        PersistentMessageBusService messageBus,
        GraniteDataContext dbContext
    )
    {
        _logger = logger;
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
            .Players.Include(p => p.Sessions)
            .Include(p => p.InventorySlots)
            .FirstOrDefaultAsync(p => p.ServerId == serverId && p.Id == playerId);

        if (player == null)
        {
            return null;
        }

        var isPlaying = player.Sessions.Any(s => s.LeaveDate == null);
        var lastSession = player.Sessions.OrderByDescending(s => s.JoinDate).FirstOrDefault();

        // Group inventory slots by inventory name
        var inventories = player
            .InventorySlots.GroupBy(slot => slot.InventoryName)
            .ToDictionary(
                g => g.Key,
                g =>
                    new InventoryDTO
                    {
                        Name = g.Key,
                        Slots = g.Select(slot => new InventorySlotDTO
                            {
                                SlotIndex = slot.SlotIndex,
                                EntityId = slot.EntityId,
                                EntityClass = slot.EntityClass,
                                Name = slot.Name,
                                StackSize = slot.StackSize,
                            })
                            .OrderBy(s => s.SlotIndex)
                            .ToList(),
                    }
            );

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
            Inventories = inventories,
        };
    }

    public async Task WhitelistPlayer(Guid serverId, string playerId, string? reason)
    {
        var whitelistPlayerCommand = _messageBus.CreateCommand<WhitelistPlayerCommand>(
            serverId,
            cmd =>
            {
                cmd.Data.PlayerId = playerId;
                cmd.Data.Reason = reason;
            }
        );

        await _messageBus.PublishCommandAsync(whitelistPlayerCommand);
    }

    public async Task UnwhitelistPlayer(Guid serverId, string playerId)
    {
        var unwhitelistPlayerCommand = _messageBus.CreateCommand<UnwhitelistPlayerCommand>(
            serverId,
            cmd =>
            {
                cmd.Data.PlayerId = playerId;
            }
        );

        await _messageBus.PublishCommandAsync(unwhitelistPlayerCommand);
    }

    public async Task BanPlayer(
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

        await _messageBus.PublishCommandAsync(banPlayerCommand);
    }

    public async Task UnbanPlayer(Guid serverId, string playerId)
    {
        var unbanPlayerCommand = _messageBus.CreateCommand<UnbanPlayerCommand>(
            serverId,
            cmd =>
            {
                cmd.Data.PlayerId = playerId;
            }
        );

        await _messageBus.PublishCommandAsync(unbanPlayerCommand);
    }

    public async Task KickPlayer(Guid serverId, string playerId, string reason)
    {
        var kickPlayerCommand = _messageBus.CreateCommand<KickPlayerCommand>(
            serverId,
            cmd =>
            {
                cmd.Data.PlayerId = playerId;
                cmd.Data.Reason = reason;
            }
        );

        await _messageBus.PublishCommandAsync(kickPlayerCommand);
    }

    public virtual async Task UpdateInventorySlot(
        Guid serverId,
        string playerUID,
        string inventoryName,
        int slotIndex,
        UpdateInventorySlotRequestDTO request
    )
    {
        var updateCommand = _messageBus.CreateCommand<UpdateInventorySlotCommand>(
            serverId,
            cmd =>
            {
                cmd.Data.PlayerId = playerUID;
                cmd.Data.InventoryName = inventoryName;
                cmd.Data.SlotIndex = slotIndex;
                cmd.Data.ItemId = request.EntityId;
                cmd.Data.Quantity = request.StackSize ?? 1;
            }
        );

        await _messageBus.PublishCommandAsync(updateCommand);
    }

    public virtual async Task RemoveInventorySlot(
        Guid serverId,
        string playerUID,
        string inventoryName,
        int slotIndex
    )
    {
        var removeCommand = _messageBus.CreateCommand<RemoveInventorySlotCommand>(
            serverId,
            cmd =>
            {
                cmd.Data.PlayerId = playerUID;
                cmd.Data.InventoryName = inventoryName;
                cmd.Data.SlotIndex = slotIndex;
            }
        );

        await _messageBus.PublishCommandAsync(removeCommand);
    }
}
