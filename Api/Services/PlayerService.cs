using System;
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
        var allPlayerData = _api.PlayerData.PlayerDataByUid.Values.ToList();
        var allServerPlayers = _api.Server.Players.ToList();
        var allBannedPlayers = PlayerDataManager.BannedPlayers;
        var allWhitelistedPlayers = PlayerDataManager.WhitelistedPlayers;

        var fullList =
            from pd in allPlayerData
            join sp in allServerPlayers on pd.PlayerUID equals sp.PlayerUID into ps
            from sp in ps.DefaultIfEmpty()
            join bp in allBannedPlayers on pd.PlayerUID equals bp.PlayerUID into bps
            from bp in bps.DefaultIfEmpty()
            join wp in allWhitelistedPlayers on pd.PlayerUID equals wp.PlayerUID into wps
            from wp in wps.DefaultIfEmpty()
            select MapToPlayerDTO(pd, sp, bp, wp);

        return await Task.FromResult(fullList.ToList());
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

    public async Task UpdatePlayerInventorySlotAsync(
        string playerId,
        string inventoryName,
        UpdateInventorySlotRequestDTO request
    )
    {
        var serverPlayer = _api.World.AllPlayers.FirstOrDefault(sp => sp.PlayerUID == playerId);
        if (serverPlayer != null)
        {
            var inventoryManager = serverPlayer.InventoryManager;

            var inventoryId = inventoryManager.GetInventoryName(inventoryName);
            var inventory = inventoryManager.GetInventory(inventoryId);
            if (inventory != null)
            {
                var slot = inventory[request.SlotIndex];
                if (slot.Empty)
                {
                    if (!request.Id.HasValue)
                    {
                        throw new ArgumentException(
                            "Item ID must be provided when adding a new item to an empty slot."
                        );
                    }
                    var block = _api.World.GetBlock(request.Id.Value);
                    slot.Itemstack = new ItemStack(block, request.StackSize ?? 1);
                }
                slot.Itemstack.StackSize = request.StackSize ?? slot.Itemstack.StackSize;

                slot.MarkDirty();
            }
        }
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
                Class = slot.Itemstack.Class.ToString(),
                Name = slot.Itemstack.GetName(),
                Id = slot.Itemstack.Id,
                StackSize = slot.Itemstack.StackSize,
            };
        }

        return new InventorySlotDTO { SlotIndex = slotIndex };
    }

    private PlayerDTO MapToPlayerDTO(
        IServerPlayerData pd,
        IServerPlayer sp,
        PlayerEntry bp,
        PlayerEntry wp
    )
    {
        var playerDto = new PlayerDTO
        {
            Id = pd.PlayerUID,
            Name = pd.LastKnownPlayername,
            IpAddress = sp?.IpAddress ?? "Offline",
            LanguageCode = sp?.LanguageCode ?? "N/A",
            ConnectionState = sp?.ConnectionState.ToString() ?? "Offline",
            Ping = sp == null || float.IsNaN(sp.Ping) ? 0 : sp.Ping,
            RolesCode = sp?.Role.Code ?? "N/A",
            FirstJoinDate = pd.FirstJoinDate,
            LastJoinDate = pd.LastJoinDate,
            Privileges = sp?.Privileges.ToArray() ?? new string[0],
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

    private async Task<string> ResolvePlayerIdByName(string name)
    {
        var player = _api.Server.Players.FirstOrDefault(p =>
            p.PlayerName.Equals(name, StringComparison.OrdinalIgnoreCase)
        );
        if (player != null)
        {
            return player.PlayerUID;
        }

        // For players the server has never seen
        var playerResponseTask = new TaskCompletionSource<string>();
        PlayerDataManager.ResolvePlayerName(
            name,
            (response, data) =>
            {
                playerResponseTask.TrySetResult(data);
            }
        );
        var playerId = await playerResponseTask.Task;
        return playerId;
    }

    private async Task<string> ResolvePlayerNameById(string id)
    {
        // Check if the server has data on the player first
        var player = (await GetAllPlayersAsync()).FirstOrDefault(p => p.Id == id);
        if (player != null)
        {
            return player.Name;
        }

        // For players the server has never seen
        var playerResponseTask = new TaskCompletionSource<string>();
        // TODO: Investigate why this returns the correct name but will cause a `System.Threading.Tasks.Task' does not contain a definition for 'Result'` to be thrown by the caller
        PlayerDataManager.ResolvePlayerUid(
            id,
            (response, data) =>
            {
                if (response == EnumServerResponse.Good)
                {
                    playerResponseTask.TrySetResult(data);
                }
            }
        );
        var playerName = await playerResponseTask.Task;
        return playerName;
    }
}
