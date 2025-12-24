using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraniteServer.Api.Models;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace GraniteServer.Api.Services;

public class PlayerService
{
    private readonly ICoreServerAPI _api;
    private readonly ServerCommandService _commandService;

    private readonly ConcurrentDictionary<string, string> _nameToIdCache =
        new ConcurrentDictionary<string, string>();

    private readonly ConcurrentDictionary<string, string> _idToNameCache =
        new ConcurrentDictionary<string, string>();

    public PlayerService(ICoreServerAPI api, ServerCommandService commandService)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
    }

    private PlayerDataManager PlayerDataManager => (PlayerDataManager)_api.PlayerData;

    /// <summary>
    /// Adds a player to the ban list.
    /// </summary>
    /// <param name="id">The unique ID of the player to add to the ban list.</param>
    /// <param name="reason">The reason for banning the player.</param>
    public async Task AddPlayerToBanListAsync(
        string id,
        string reason,
        string issuedBy = "API",
        DateTime? untilDate = null
    )
    {
        var currentBannedPlayers = await GetBannedPlayersAsync();
        if (currentBannedPlayers.Any(p => p.Id == id))
        {
            return;
        }
        var playerName = await ResolvePlayerNameById(id);
        // TODO: revisit if PlayerDataManager.BanPlayer method changes from internal to public
        PlayerDataManager.BannedPlayers.Add(
            new PlayerEntry
            {
                PlayerUID = id,
                PlayerName = playerName,
                Reason = reason,
                IssuedByPlayerName = issuedBy,
                UntilDate = untilDate ?? DateTime.MaxValue,
            }
        );

        PlayerDataManager.bannedListDirty = true;
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
        var allPlayerData = _api.PlayerData.PlayerDataByUid;
        var allServerPlayers = _api.Server.Players.ToDictionary(p => p.PlayerUID);
        var allBannedPlayers = PlayerDataManager.BannedPlayers;
        var allWhitelistedPlayers = PlayerDataManager.WhitelistedPlayers;

        // Build quick lookups keyed by player ID
        var bannedById = allBannedPlayers
            .GroupBy(bp => bp.PlayerUID)
            .ToDictionary(g => g.Key, g => g.First());
        var whitelistedById = allWhitelistedPlayers
            .GroupBy(wp => wp.PlayerUID)
            .ToDictionary(g => g.Key, g => g.First());

        // Union all known player IDs from any source (seen, online, banned, whitelisted)
        var allIds = new HashSet<string>(allPlayerData.Keys);
        allIds.UnionWith(allServerPlayers.Keys);
        allIds.UnionWith(bannedById.Keys);
        allIds.UnionWith(whitelistedById.Keys);

        var fullList = allIds
            .Select(id =>
            {
                allPlayerData.TryGetValue(id, out var pd);
                allServerPlayers.TryGetValue(id, out var sp);
                bannedById.TryGetValue(id, out var bp);
                whitelistedById.TryGetValue(id, out var wp);
                return MapToPlayerDTO(id, pd, sp, bp, wp);
            })
            .ToList();

        return await Task.FromResult(fullList);
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
            var inventoryManager = serverPlayer.InventoryManager;

            foreach (var inventoryEntry in inventoryManager.Inventories)
            {
                if (inventoryEntry.Value.ClassName == "creative")
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
    public async Task KickPlayerAsync(
        string playerId,
        string reason,
        bool waitForDisconnect = false
    )
    {
        var player = _api.Server.Players.Where(p => p.PlayerUID == playerId).FirstOrDefault();
        if (player != null)
        {
            try
            {
                // player.Disconnect(reason);
                await _commandService.KickUserAsync(player.PlayerName, reason);
            }
            catch (Exception)
            {
                // Handle exception
            }

            if (waitForDisconnect)
            {
                // Wait up to 5 seconds for the player to disconnect
                int attempts = 0;
                var isDisconnected = false;
                do
                {
                    isDisconnected =
                        (await GetAllPlayersAsync()).Single(p => p.Id == playerId).ConnectionState
                        == "Offline";
                    await Task.Delay(500);
                    attempts++;
                } while (!isDisconnected && attempts < 10);
            }
        }
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

        // TODO: revisit if PlayerDataManager.UnBanPlayer method changes from internal to public
        PlayerDataManager.BannedPlayers.RemoveAll(pe => pe.PlayerUID == id);
        PlayerDataManager.bannedListDirty = true;
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

    private PlayerDTO MapToPlayerDTO(
        string playerId,
        IServerPlayerData? pd,
        IServerPlayer? sp,
        PlayerEntry? bp,
        PlayerEntry? wp
    )
    {
        var resolvedName =
            pd?.LastKnownPlayername
            ?? sp?.PlayerName
            ?? bp?.PlayerName
            ?? wp?.PlayerName
            ?? playerId;

        var playerDto = new PlayerDTO
        {
            Id = playerId,
            Name = resolvedName,
            IpAddress = sp?.IpAddress ?? "Offline",
            LanguageCode = sp?.LanguageCode ?? "N/A",
            ConnectionState = sp?.ConnectionState.ToString() ?? "Offline",
            Ping = sp == null || float.IsNaN(sp.Ping) ? 0 : sp.Ping,
            RolesCode = sp?.Role.Code ?? "N/A",
            FirstJoinDate = pd?.FirstJoinDate ?? string.Empty,
            LastJoinDate = pd?.LastJoinDate ?? string.Empty,
            Privileges = sp?.Privileges.ToArray() ?? Array.Empty<string>(),
            IsAdmin = false,
            IsBanned = bp != null,
            BanReason = bp?.Reason,
            BanBy = bp?.IssuedByPlayerName,
            BanUntil = bp?.UntilDate,
            IsWhitelisted = wp != null,
            WhitelistedReason = wp?.Reason,
            WhitelistedBy = wp?.IssuedByPlayerName,
            WhitelistedUntil = wp?.UntilDate,
        };

        return playerDto;
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
