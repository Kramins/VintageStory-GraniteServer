using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenHTTP.Api.Protocol;
using GenHTTP.Modules.Controllers;
using GenHTTP.Modules.Reflection;
using GenHTTP.Modules.Webservices;
using GraniteServer.Api.Extensions;
using GraniteServer.Api.Models;
using GraniteServer.Api.Models.JsonApi;
using GraniteServer.Api.Services;
using Sieve.Models;
using Sieve.Services;
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
    private readonly SieveProcessor _sieve;

    public PlayerManagementController(
        PlayerService playerService,
        SieveProcessor sieve,
        ICoreServerAPI api
    )
    {
        _playerService = playerService ?? throw new ArgumentNullException(nameof(playerService));
        _sieve = sieve ?? throw new ArgumentNullException(nameof(sieve));
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    /// <summary>
    /// Adds a player to the whitelist by their ID.
    /// </summary>
    /// <param name="id">The ID of the player to whitelist.</param>
    [ResourceMethod(RequestMethod.Post, "/id/:playerId/whitelist")]
    public async Task AddToWhitelist(string playerId)
    {
        await _playerService.AddPlayerToWhitelistAsync(playerId);
    }

    [ResourceMethod(RequestMethod.Post, "/id/:playerId/ban")]
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
    public async Task<JsonApiDocument<IList<PlayerDTO>>> GetAllPlayers(
        int page = 0,
        int pageSize = 20,
        string sorts = "id",
        string filters = ""
    )
    {
        try
        {
            var sieveModel = new SieveModel
            {
                Filters = filters,
                Sorts = sorts,
                Page = page,
                PageSize = pageSize,
            };
            var completeList = await _playerService.GetAllPlayersAsync();
            var query = completeList.AsQueryable();

            var totalCount = query.Count();
            query = _sieve.Apply(sieveModel, query);
            var paged = query.ToList();

            return new JsonApiDocument<IList<PlayerDTO>>
            {
                Data = paged,
                Meta = new JsonApiMeta
                {
                    Pagination = new PaginationMeta
                    {
                        Page = page,
                        PageSize = pageSize,
                        HasMore = paged.Count >= pageSize,
                        TotalCount = totalCount,
                    },
                },
            };
        }
        catch (Exception ex)
        {
            _api.Logger.Warning("Error retrieving all players: " + ex.Message);
            // Preserve existing behavior by rethrowing; clients will receive an error status
            throw;
        }
    }

    [ResourceMethod(RequestMethod.Get, "/id/:playerId")]
    public async Task<PlayerDetailsDTO> GetPlayerDetailsAsync(string playerId)
    {
        return await _playerService.GetPlayerDetailsAsync(playerId);
    }

    /// <summary>
    /// Kicks a player by their ID.
    /// </summary>
    /// <param name="id">The ID of the player to kick.</param>
    /// <param name="request">The kick request containing the reason.</param>
    [ResourceMethod(RequestMethod.Post, "/id/:playerId/kick")]
    public async Task<string> Kick(string playerId, KickRequestDTO request)
    {
        return await _playerService.KickPlayerAsync(
            playerId,
            request.Reason ?? "Kicked by an administrator.",
            true
        );
    }

    /// <summary>
    /// Removes a player from the ban list by their ID.
    /// </summary>
    /// <param name="id">The ID of the player to remove from the ban list.</param>
    [ResourceMethod(RequestMethod.Delete, "/id/:playerId/ban")]
    public async Task RemoveFromBanList(string playerId)
    {
        await _playerService.RemovePlayerFromBanListAsync(playerId);
    }

    /// <summary>
    /// Removes a player from the whitelist by their ID.
    /// </summary>
    /// <param name="id">The ID of the player to remove from the whitelist.</param>
    [ResourceMethod(RequestMethod.Delete, "/id/:playerId/whitelist")]
    public async Task RemoveFromWhitelist(string playerId)
    {
        await _playerService.RemovePlayerFromWhitelistAsync(playerId);
    }

    [ResourceMethod(RequestMethod.Delete, "/id/:playerId/inventories/:inventoryName/:slotIndex")]
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
    [ResourceMethod(RequestMethod.Post, "/id/:playerId/inventories/:inventoryName")]
    public async Task UpdatePlayerInventorySlotAsync(
        string playerId,
        string inventoryName,
        UpdateInventorySlotRequestDTO request
    )
    {
        await _playerService.UpdatePlayerInventorySlotAsync(playerId, inventoryName, request);
    }

    /// <summary>
    /// Lists sessions for a given player.
    /// </summary>
    /// <param name="playerId">Player ID</param>
    /// <param name="page">Zero-based page index</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="sorts">Sort column (id, joinDate, leaveDate, serverName). Defaults to id.</param>
    /// <param name="filters">Filter expression</param>
    [ResourceMethod(RequestMethod.Get, "/id/:playerId/sessions")]
    public JsonApiDocument<IList<PlayerSessionDTO>> GetPlayerSessions(
        string playerId,
        int page = 0,
        int pageSize = 20,
        string sorts = "id",
        string filters = ""
    )
    {
        var sieveModel = new SieveModel
        {
            Filters = filters,
            Sorts = sorts,
            Page = page,
            PageSize = pageSize,
        };

        var completeQuery = _playerService.GetPlayerSessions(playerId);
        var totalCount = completeQuery.Count();
        var query = _sieve.Apply(sieveModel, completeQuery);
        var sessions = query.ToList();

        return new JsonApiDocument<IList<PlayerSessionDTO>>
        {
            Data = sessions,
            Meta = new JsonApiMeta
            {
                Pagination = new PaginationMeta
                {
                    Page = page,
                    PageSize = pageSize,
                    HasMore = sessions.Count >= pageSize,
                    TotalCount = totalCount,
                },
            },
        };
    }
}
