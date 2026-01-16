using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraniteServer.Api.Messaging.Contracts;
using GraniteServer.Api.Models;
using GraniteServer.Common;
using GraniteServer.Data;
using GraniteServer.Messaging.Commands;
using GraniteServer.Messaging.Events;
using Microsoft.EntityFrameworkCore;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace GraniteServer.Api.Services;

public class PlayerService
{
    private readonly ICoreServerAPI _api;
    private readonly VintageStoryProxyResolver _proxyResolver;
    private readonly MessageBusService _messageBus;
    private readonly GraniteDataContext _dataContext;
    private readonly ConcurrentDictionary<string, string> _nameToIdCache =
        new ConcurrentDictionary<string, string>();

    private readonly ConcurrentDictionary<string, string> _idToNameCache =
        new ConcurrentDictionary<string, string>();

    public PlayerService(
        ICoreServerAPI api,
        VintageStoryProxyResolver vintageStoryProxyResolver,
        MessageBusService messageBus,
        GraniteDataContext dataContext
    )
    {
        _api = api;
        _proxyResolver = vintageStoryProxyResolver;
        _messageBus = messageBus;
        _dataContext = dataContext;
    }

    private PlayerDataManager PlayerDataManager => (PlayerDataManager)_api.PlayerData;

    /// <summary>
    /// Adds a player to the ban list.
    /// </summary>
    /// <param name="playerId">The unique ID of the player to add to the ban list.</param>
    /// <param name="reason">The reason for banning the player.</param>
    public async Task AddPlayerToBanListAsync(
        string playerId,
        string reason,
        string issuedBy = "API",
        DateTime? untilDate = null
    )
    {
        var currentBannedPlayers = await GetBannedPlayersAsync();
        if (currentBannedPlayers.Any(p => p.Id == playerId))
        {
            return;
        }
        var playerName = await ResolvePlayerNameById(playerId);

        _messageBus.Publish(
            new BanPlayerCommand
            {
                Data = new BanPlayerCommandData
                {
                    PlayerId = playerId,
                    PlayerName = playerName,
                    Reason = reason,
                    IssuedBy = issuedBy,
                    ExpirationDate = untilDate,
                },
            }
        );
    }

    /// <summary>
    /// Adds a player to the whitelist.
    /// </summary>
    /// <param name="id">The unique ID of the player to add to the whitelist.</param>
    public async Task AddPlayerToWhitelistAsync(string id)
    {
        var currentWhitelistedPlayers = await GetWhitelistedPlayersAsync();
        if (currentWhitelistedPlayers.Any(p => p.Id == id))
        {
            return;
        }
        var playerName = await ResolvePlayerNameById(id);
        PlayerDataManager.WhitelistPlayer(playerName, id, "Added via API");
        _messageBus.Publish(
            new PlayerWhitelistedEvent()
            {
                Data = new PlayerWhitelistedEventData { PlayerId = id, PlayerName = playerName },
            }
        );
    }

    public async Task<PlayerNameIdDTO> FindPlayerByNameAsync(string name)
    {
        var playerId = await ResolvePlayerIdByName(name);
        return new PlayerNameIdDTO { Id = playerId, Name = name };
    }

    /// <summary>
    /// Retrieves a list of all players, including their details such as connection state, roles, and privileges.
    /// </summary>
    /// <returns>A list of PlayerDTO objects representing all players.</returns>
    public async Task<List<PlayerDTO>> GetAllPlayersAsync()
    {
        var proxy = _proxyResolver.GetProxy();
        var snapshots = await proxy.GetAllPlayersAsync();

        return snapshots.Select(MapSnapshotToPlayerDTO).ToList();
    }

    /// <summary>
    /// Retrieves a list of all players who are banned from the server.
    /// </summary>
    /// <returns>A list of PlayerDTO objects representing banned players.</returns>
    public async Task<IList<PlayerDTO>> GetBannedPlayersAsync()
    {
        var players = await GetAllPlayersAsync();
        return players.Where(p => p.IsBanned).ToList();
    }

    /// <summary>
    /// Retrieves a player's details by their unique player ID.
    /// </summary>
    /// <param name="playerId">The unique ID of the player.</param>
    /// <returns>A PlayerDTO object representing the player, or null if not found.</returns>
    public async Task<PlayerDTO?> GetPlayerByIdAsync(string playerId)
    {
        var players = await GetAllPlayersAsync();
        var player = players.Where(p => p.Id == playerId).FirstOrDefault();
        return player;
    }

    public async Task<PlayerDetailsDTO?> GetPlayerDetailsAsync(string playerId)
    {
        var player = await GetPlayerByIdAsync(playerId);

        if (player == null)
        {
            return null;
        }

        var playerDetails = new PlayerDetailsDTO
        {
            Id = player.Id,
            Name = player.Name,
            IpAddress = player.IpAddress,
            LanguageCode = player.LanguageCode,
            ConnectionState = player.ConnectionState,
            Ping = player.Ping,
            RolesCode = player.RolesCode,
            FirstJoinDate = player.FirstJoinDate,
            LastJoinDate = player.LastJoinDate,
            Privileges = player.Privileges,
            IsAdmin = player.IsAdmin,
            IsBanned = player.IsBanned,
            BanReason = player.BanReason,
            BanBy = player.BanBy,
            BanUntil = player.BanUntil,
            IsWhitelisted = player.IsWhitelisted,
            WhitelistedReason = player.WhitelistedReason,
            WhitelistedBy = player.WhitelistedBy,
            WhitelistedUntil = player.WhitelistedUntil,
        };

        // NOTE: Player needs to have logged into the server at least once for inventories to be available, I don't know how to get inventories for players the server has never seen
        // Server restart clear the AllPlayers list, so inventories are only available for currently connected players
        var serverPlayer = _api.World.AllPlayers.FirstOrDefault(sp => sp.PlayerUID == playerId);
        if (serverPlayer != null)
        {
            var validInventoryClass = new HashSet<string> { "hotbar", "backpack", "character" };
            var inventoryManager = serverPlayer.InventoryManager;

            foreach (var inventoryEntry in inventoryManager.Inventories)
            {
                if (!validInventoryClass.Contains(inventoryEntry.Value.ClassName))
                {
                    // Skip creative inventory
                    continue;
                }
                var inventoryDto = MapToInventoryDTO(
                    inventoryEntry.Value.ClassName,
                    inventoryEntry.Value
                );
                playerDetails.Inventories.Add(inventoryDto.Name, inventoryDto);
            }
        }

        return playerDetails;
    }

    /// <summary>
    /// Retrieves player sessions for a given player ID, ordered by join date descending.
    /// Supports pagination via page and pageSize.
    /// </summary>
    /// <param name="playerId">The player ID to filter sessions.</param>
    /// <param name="page">Zero-based page index.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>A list of PlayerSessionDTO.</returns>
    public IQueryable<PlayerSessionDTO> GetPlayerSessions(string playerId)
    {
        var query =
            from ps in _dataContext.PlayerSessions
            join s in _dataContext.Servers on ps.ServerId equals s.Id into ss
            from s in ss.DefaultIfEmpty()
            where ps.PlayerId == playerId
            select new PlayerSessionDTO
            {
                Id = ps.Id,
                PlayerId = ps.PlayerId,
                ServerId = ps.ServerId,
                ServerName = s != null ? s.Name : string.Empty,
                JoinDate = ps.JoinDate,
                LeaveDate = ps.LeaveDate,
                IpAddress = ps.IpAddress,
                PlayerName = ps.PlayerName,
                Duration = ps.Duration,
                IsActive = !ps.LeaveDate.HasValue,
            };

        return query;
    }

    /// <summary>
    /// Retrieves a list of all players who are whitelisted on the server.
    /// </summary>
    /// <returns>A list of PlayerDTO objects representing whitelisted players.</returns>
    public async Task<IList<PlayerDTO>> GetWhitelistedPlayersAsync()
    {
        var players = await GetAllPlayersAsync();
        return players.Where(p => p.IsWhitelisted).ToList();
    }

    /// <summary>
    /// Disconnects a player from the server with a specified reason.
    /// </summary>
    /// <param name="playerId">The unique ID of the player to disconnect.</param>
    /// <param name="reason">The reason for disconnecting the player.</param>
    public async Task<string> KickPlayerAsync(
        string playerId,
        string reason,
        bool waitForDisconnect = false
    )
    {
        var command = new KickPlayerCommand
        {
            Data = new KickPlayerCommandData
            {
                PlayerId = playerId,
                Reason = reason,
                WaitForDisconnect = waitForDisconnect,
            },
        };

        _messageBus.Publish(command);

        return $"Kick command for player {playerId} has been issued.";
    }

    /// <summary>
    /// Removes a player from the ban list.
    /// </summary>
    /// <param name="id">The unique ID of the player to remove from the ban list.</param>
    public async Task RemovePlayerFromBanListAsync(string id)
    {
        var currentBannedPlayers = await GetBannedPlayersAsync();
        if (!currentBannedPlayers.Any(p => p.Id == id))
        {
            return;
        }
        var playerName = await ResolvePlayerNameById(id);

        _messageBus.Publish(
            new UnbanPlayerCommand { Data = new UnbanPlayerCommandData { PlayerId = id } }
        );
    }

    /// <summary>
    /// Removes a player from the whitelist.
    /// </summary>
    /// <param name="id">The unique ID of the player to remove from the whitelist.</param>
    public async Task RemovePlayerFromWhitelistAsync(string id)
    {
        var currentWhitelistedPlayers = await GetWhitelistedPlayersAsync();
        if (!currentWhitelistedPlayers.Any(p => p.Id == id))
        {
            return;
        }
        var playerName = await ResolvePlayerNameById(id);
        PlayerDataManager.UnWhitelistPlayer(playerName, id);

        _messageBus.Publish(
            new PlayerUnwhitelistedEvent()
            {
                Data = new PlayerUnwhitelistedEventData { PlayerId = id, PlayerName = playerName },
            }
        );
    }

    public async Task RemovePlayerInventoryFromSlotAsync(
        string playerId,
        string inventoryName,
        int slotIndex
    )
    {
        var inventoryManager = GetPlayerInventoryManager(playerId);
        var inventoryId = inventoryManager.GetInventoryName(inventoryName);
        var inventory = inventoryManager.GetInventory(inventoryId);

        if (inventory != null)
        {
            var slot = inventory[slotIndex];
            slot.Itemstack = null;
            slot.MarkDirty();
        }
    }

    public async Task UpdatePlayerInventorySlotAsync(
        string playerId,
        string inventoryName,
        UpdateInventorySlotRequestDTO request
    )
    {
        if (string.IsNullOrEmpty(request.EntityClass))
            throw new ArgumentException("Item class must be provided.");

        var inventoryManager = GetPlayerInventoryManager(playerId);
        var inventoryId = inventoryManager.GetInventoryName(inventoryName);
        var inventory = inventoryManager.GetInventory(inventoryId);

        if (inventory != null)
        {
            var slot = inventory[request.SlotIndex];

            ItemStack newItemStack;
            CollectibleObject newCollectible;
            switch (request.EntityClass.ToLower())
            {
                case "item":
                    newCollectible = _api.World.GetItem(request.EntityId);
                    break;
                case "block":
                    newCollectible = _api.World.GetBlock(request.EntityId);
                    break;
                default:
                    throw new ArgumentException(
                        "Invalid item class specified. Must be 'item' or 'block'."
                    );
            }

            if (newCollectible == null)
                throw new ArgumentException(
                    $"{request.EntityClass} with ID {request.EntityId} not found."
                );

            var newStackSize = request.StackSize ?? 1;
            if (newStackSize > newCollectible.MaxStackSize)
            {
                newStackSize = newCollectible.MaxStackSize;
            }
            newItemStack = new ItemStack(newCollectible, newStackSize);

            slot.Itemstack = newItemStack;

            slot.MarkDirty();
        }
    }

    private IPlayerInventoryManager GetPlayerInventoryManager(string playerId)
    {
        var serverPlayer = _api.World.AllPlayers.FirstOrDefault(sp => sp.PlayerUID == playerId);
        if (serverPlayer != null)
        {
            return serverPlayer.InventoryManager;
        }
        throw new ArgumentException($"Player with ID {playerId} not found or not online.");
    }

    private Dictionary<string, object>? MapToAttributesDictionary(ITreeAttribute? attributes)
    {
        if (attributes == null)
            return null;

        var dict = new Dictionary<string, object>();
        var attrString = attributes.ToString();
        if (!string.IsNullOrEmpty(attrString))
        {
            dict["data"] = attrString;
        }

        return dict.Count > 0 ? dict : null;
    }

    private InventoryDTO MapToInventoryDTO(string name, IInventory inventory)
    {
        var slots = new List<InventorySlotDTO>();

        for (int i = 0; i < inventory.Count; i++)
        {
            var slot = inventory[i];
            if (slot != null)
            {
                var slotDto = MapToInventorySlotDTO(i, slot);
                slots.Add(slotDto);
            }
        }

        return new InventoryDTO { Name = name, Slots = slots };
    }

    private InventorySlotDTO MapToInventorySlotDTO(int slotIndex, ItemSlot slot)
    {
        slot.GetStackName();

        if (!slot.Empty)
        {
            return new InventorySlotDTO
            {
                SlotIndex = slotIndex,
                EntityClass = slot.Itemstack.Class.ToString(),
                Name = slot.Itemstack.GetName(),
                EntityId = slot.Itemstack.Id,
                StackSize = slot.Itemstack.StackSize,
            };
        }

        return new InventorySlotDTO { SlotIndex = slotIndex };
    }

    private PlayerDTO MapSnapshotToPlayerDTO(DetailedPlayerSnapshot snapshot)
    {
        return new PlayerDTO
        {
            Id = snapshot.Id,
            Name = snapshot.Name,
            IpAddress = snapshot.IpAddress,
            LanguageCode = snapshot.LanguageCode,
            ConnectionState = snapshot.ConnectionState,
            Ping = snapshot.Ping,
            RolesCode = snapshot.RolesCode,
            FirstJoinDate = snapshot.FirstJoinDate,
            LastJoinDate = snapshot.LastJoinDate,
            Privileges = snapshot.Privileges,
            IsAdmin = snapshot.IsAdmin,
            IsBanned = snapshot.IsBanned,
            BanReason = snapshot.BanReason,
            BanBy = snapshot.BanBy,
            BanUntil = snapshot.BanUntil,
            IsWhitelisted = snapshot.IsWhitelisted,
            WhitelistedReason = snapshot.WhitelistedReason,
            WhitelistedBy = snapshot.WhitelistedBy,
            WhitelistedUntil = snapshot.WhitelistedUntil,
        };
    }

    private async Task<string> ResolvePlayerIdByName(string name, bool useCache = true)
    {
        var loweredName = name.ToLower();
        var player = _api.Server.Players.FirstOrDefault(p =>
            p.PlayerName.Equals(name, StringComparison.OrdinalIgnoreCase)
        );
        if (player != null)
        {
            return player.PlayerUID;
        }

        if (useCache && _nameToIdCache.TryGetValue(loweredName, out var cachedId))
        {
            return cachedId;
        }

        // For players the server has never seen, we want to minimize calls to AuthServerComm
        var playerResponseTask = new TaskCompletionSource<string>();
        AuthServerComm.ResolvePlayerName(
            name,
            (response, data) =>
            {
                playerResponseTask.TrySetResult(data);
            }
        );

        var playerId = await playerResponseTask.Task;
        _nameToIdCache.AddOrUpdate(loweredName, playerId, (_, __) => playerId);
        _idToNameCache.AddOrUpdate(playerId, loweredName, (_, __) => loweredName);
        return playerId;
    }

    private async Task<string> ResolvePlayerNameById(string id, bool useCache = true)
    {
        // Check if the server has data on the player first
        var player = (await GetAllPlayersAsync()).FirstOrDefault(p => p.Id == id);
        if (player != null)
        {
            return player.Name;
        }

        if (useCache && _idToNameCache.TryGetValue(id, out var cachedName))
        {
            return cachedName;
        }

        // For players the server has never seen, we want to minimize calls to AuthServerComm
        var playerResponseTask = new TaskCompletionSource<string>();
        AuthServerComm.ResolvePlayerUid(
            id,
            (response, data) =>
            {
                playerResponseTask.TrySetResult(data);
            }
        );

        var playerName = await playerResponseTask.Task;
        var loweredName = playerName.ToLower();
        _idToNameCache.AddOrUpdate(id, loweredName, (_, __) => loweredName);
        _nameToIdCache.AddOrUpdate(loweredName, id, (_, __) => id);

        return playerName;
    }
}
