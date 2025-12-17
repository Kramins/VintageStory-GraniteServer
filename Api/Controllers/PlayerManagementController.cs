using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenHTTP.Api.Protocol;
using GenHTTP.Modules.Controllers;
using GenHTTP.Modules.Reflection;
using GenHTTP.Modules.Webservices;
using GraniteServer.Api.Models;
using GraniteServer.Api.Services;
using Vintagestory.API.Server;

namespace GraniteServer.Api;

/// <summary>
/// Player control and administration controller
/// Base URL: /api/players
/// </summary>
public class PlayerManagementController
{
    private readonly ICoreServerAPI _api;
    private readonly PlayerService _playerService;

    public PlayerManagementController(PlayerService playerService, ICoreServerAPI api)
    {
        _playerService = playerService ?? throw new ArgumentNullException(nameof(playerService));
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    /// <summary>
    /// Adds a player to the whitelist by their ID.
    /// </summary>
    /// <param name="id">The ID of the player to whitelist.</param>
    [ResourceMethod(RequestMethod.Post, "/:playerId/whitelist")]
    public async Task AddToWhitelist(string playerId)
    {
        await _playerService.AddPlayerToWhitelistAsync(playerId);
    }

    [ResourceMethod(RequestMethod.Post, "/:playerId/ban")]
    public async Task BanPlayer(string playerId, BanRequestDTO request)
    {
        await _playerService.AddPlayerToBanListAsync(
            playerId,
            request.Reason ?? "Banned by an administrator.",
            request.IssuedBy ?? "API",
            request.UntilDate
        );
    }

    [ResourceMethod(RequestMethod.Get, "/find")]
    public async Task<PlayerNameIdDTO> FindPlayerByName(string name)
    {
        return await _playerService.FindPlayerByNameAsync(name);
    }

    /// <summary>
    /// Lists all connected players.
    /// </summary>
    /// <returns>A list of all connected players.</returns>
    [ResourceMethod(RequestMethod.Get)]
    public async Task<Result<IList<PlayerDTO>>> GetAllPlayers()
    {
        try
        {
            var result = await _playerService.GetAllPlayersAsync();
            return new Result<IList<PlayerDTO>>(result);
        }
        catch (Exception ex)
        {
            _api.Logger.Warning("Error retrieving all players: " + ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Lists all banned players.
    /// </summary>
    /// <returns>A list of all banned players.</returns>
    [ResourceMethod(RequestMethod.Get, "/banned")]
    public async Task<IList<PlayerDTO>> GetBannedPlayers()
    {
        return await _playerService.GetBannedPlayersAsync();
    }

    [ResourceMethod(RequestMethod.Get, "/:playerId")]
    public async Task<PlayerDetailsDTO> GetPlayerDetailsAsync(string playerId)
    {
        return await _playerService.GetPlayerDetailsAsync(playerId);
    }

    /// <summary>
    /// Lists all whitelisted players.
    /// </summary>
    /// <returns>A list of all whitelisted players.</returns>
    [ResourceMethod(RequestMethod.Get, "/whitelisted")]
    public async Task<IList<PlayerDTO>> GetWhitelistedPlayers()
    {
        return await _playerService.GetWhitelistedPlayersAsync();
    }

    /// <summary>
    /// Kicks a player by their ID.
    /// </summary>
    /// <param name="id">The ID of the player to kick.</param>
    /// <param name="request">The kick request containing the reason.</param>
    [ResourceMethod(RequestMethod.Post, "/:playerId/kick")]
    public async Task Kick(string playerId, KickRequestDTO request)
    {
        var t = _playerService.KickPlayerAsync(
            playerId,
            request.Reason ?? "Kicked by an administrator.",
            true
        );

        await t;
    }

    /// <summary>
    /// Removes a player from the ban list by their ID.
    /// </summary>
    /// <param name="id">The ID of the player to remove from the ban list.</param>
    [ResourceMethod(RequestMethod.Delete, "/:playerId/ban")]
    public async Task RemoveFromBanList(string playerId)
    {
        await _playerService.RemovePlayerFromBanListAsync(playerId);
    }

    /// <summary>
    /// Removes a player from the whitelist by their ID.
    /// </summary>
    /// <param name="id">The ID of the player to remove from the whitelist.</param>
    [ResourceMethod(RequestMethod.Delete, "/:playerId/whitelist")]
    public async Task RemoveFromWhitelist(string playerId)
    {
        await _playerService.RemovePlayerFromWhitelistAsync(playerId);
    }

    [ResourceMethod(RequestMethod.Delete, "/:playerId/inventories/:inventoryName/:slotIndex")]
    public async Task RemovePlayerInventorySlotAsync(
        string playerId,
        string inventoryName,
        int slotIndex
    )
    {
        await _playerService.RemovePlayerInventoryFromSlotAsync(playerId, inventoryName, slotIndex);
    }

    /// <summary>
    /// Updates a player's inventory slot.
    /// </summary>
    /// <param name="playerId">The ID of the player whose inventory slot is to be updated.</param>
    /// <param name="inventoryName">The name of the inventory to update.</param>
    /// <param name="request">The update request containing slot index, item ID, and stack size.</param>
    [ResourceMethod(RequestMethod.Post, "/:playerId/inventories/:inventoryName")]
    public async Task UpdatePlayerInventorySlotAsync(
        string playerId,
        string inventoryName,
        UpdateInventorySlotRequestDTO request
    )
    {
        await _playerService.UpdatePlayerInventorySlotAsync(playerId, inventoryName, request);
    }
}
