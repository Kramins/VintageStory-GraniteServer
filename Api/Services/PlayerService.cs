using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraniteServer.Api.Models;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace GraniteServer.Api.Services;

public class PlayerService
{
    private readonly ICoreServerAPI _api;

    public PlayerService(ICoreServerAPI api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    private PlayerDataManager PlayerDataManager => (PlayerDataManager)_api.PlayerData;

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
            Ping = sp?.Ping ?? 0,
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

    /// <summary>
    /// Disconnects a player from the server with a specified reason.
    /// </summary>
    /// <param name="playerId">The unique ID of the player to disconnect.</param>
    /// <param name="reason">The reason for disconnecting the player.</param>
    public async Task KickPlayerAsync(string playerId, string reason)
    {
        var player = _api.Server.Players.Where(p => p.PlayerUID == playerId).FirstOrDefault();
        if (player != null)
        {
            try
            {
                await Task.Run(() => player.Disconnect(reason));
            }
            catch (Exception)
            {
                // Handle exception
            }
        }
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
    /// Retrieves a list of all players who are banned from the server.
    /// </summary>
    /// <returns>A list of PlayerDTO objects representing banned players.</returns>
    public async Task<IList<PlayerDTO>> GetBannedPlayersAsync()
    {
        var players = await GetAllPlayersAsync();
        return players.Where(p => p.IsBanned).ToList();
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
        await Task.Run(() => PlayerDataManager.UnWhitelistPlayer(id, playerName));
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

    public async Task<PlayerNameIdDTO> FindPlayerByNameAsync(string name)
    {
        var playerId = await ResolvePlayerIdByName(name);
        return new PlayerNameIdDTO { Id = playerId, Name = name };
    }
}
