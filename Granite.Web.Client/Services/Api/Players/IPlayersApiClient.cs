using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;

namespace Granite.Web.Client.Services.Api;

/// <summary>
/// Interface for Players API client.
/// </summary>
public interface IPlayersApiClient
{
    /// <summary>
    /// Gets all players from the server.
    /// </summary>
    Task<JsonApiDocument<List<PlayerDTO>>> GetPlayersAsync(string serverId, string? filter = null, int pageSize = 20, int pageNumber = 1);

    /// <summary>
    /// Gets a specific player by UID.
    /// </summary>
    Task<JsonApiDocument<PlayerDTO>> GetPlayerAsync(string serverId, string playerUid);

    /// <summary>
    /// Gets detailed player information including inventories by UID.
    /// </summary>
    Task<JsonApiDocument<PlayerDetailsDTO>> GetPlayerDetailsAsync(string serverId, string playerUid);

    
    /// <summary>
    /// Kicks a player from the server.
    /// </summary>
    Task<JsonApiDocument<object>> KickPlayerAsync(string serverId, string playerUid, string? reason = null);

    /// <summary>
    /// Bans a player from the server.
    /// </summary>
    Task<JsonApiDocument<object>> BanPlayerAsync(string serverId, string playerUid, string? reason = null);

    /// <summary>
    /// Adds a player to the whitelist.
    /// </summary>
    Task<JsonApiDocument<object>> WhitelistPlayerAsync(string serverId, string playerUid);

    /// <summary>
    /// Removes a player from the whitelist.
    /// </summary>
    Task<JsonApiDocument<object>> RemoveFromWhitelistAsync(string serverId, string playerUid);

    /// <summary>
    /// Finds a player by name, searching the Vintage Story user database as a fallback.
    /// </summary>
    Task<JsonApiDocument<List<PlayerDTO>>> FindPlayerByNameAsync(string serverId, string playerName);
}
