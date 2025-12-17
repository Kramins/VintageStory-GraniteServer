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
    private readonly PlayerService _playerService;
    private readonly ICoreServerAPI _api;

    public PlayerManagementController(PlayerService playerService, ICoreServerAPI api)
    {
        _playerService = playerService ?? throw new ArgumentNullException(nameof(playerService));
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    /// <summary>
    /// Adds a player to the whitelist by their ID.
    /// </summary>
    /// <param name="id">The ID of the player to whitelist.</param>
    [ResourceMethod(RequestMethod.Post, "/:id/whitelist")]
    public async Task AddToWhitelist(string id)
    {
        await _playerService.AddPlayerToWhitelistAsync(id);
    }

    [ResourceMethod(RequestMethod.Post, "/:id/ban")]
    public async Task BanPlayer(string id, BanRequestDTO request)
    {
        await _playerService.AddPlayerToBanListAsync(
            id,
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
    /// Kicks a player by their ID.
    /// </summary>
    /// <param name="id">The ID of the player to kick.</param>
    /// <param name="request">The kick request containing the reason.</param>
    [ResourceMethod(RequestMethod.Post, "/:id/kick")]
    public async Task Kick(string id, KickRequestDTO request)
    {
        var t =  _playerService.KickPlayerAsync(id, request.Reason ?? "Kicked by an administrator.", true);

        await t;
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

    /// <summary>
    /// Lists all connected players.
    /// </summary>
    /// <returns>A list of all connected players.</returns>
    [ResourceMethod(RequestMethod.Get)]
    public async Task<Result<IList<PlayerDTO>>> GetAllPlayers()
    {
        try {
            var result =  await _playerService.GetAllPlayersAsync();
            return new Result<IList<PlayerDTO>>(result);
        }
        catch (Exception ex)
        {
            _api.Logger.Warning("Error retrieving all players: " + ex.Message);
            throw;
        }
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

    [ResourceMethod(RequestMethod.Delete, "/:id/ban")]
    public async Task RemoveFromBanList(string id)
    {
        await _playerService.RemovePlayerFromBanListAsync(id);
    }

    /// <summary>
    /// Removes a player from the whitelist by their ID.
    /// </summary>
    /// <param name="id">The ID of the player to remove from the whitelist.</param>
    [ResourceMethod(RequestMethod.Delete, "/:id/whitelist")]
    public async Task RemoveFromWhitelist(string id)
    {
        await _playerService.RemovePlayerFromWhitelistAsync(id);
    }
}
