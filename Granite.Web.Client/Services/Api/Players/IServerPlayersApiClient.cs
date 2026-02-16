using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;

namespace Granite.Web.Client.Services.Api;

/// <summary>
/// API client for managing players on a specific server.
/// This service handles operations for server-specific players, not global players.
/// </summary>
public interface IServerPlayersApiClient
{
    /// <summary>
    /// Gets all players currently on the specified server.
    /// </summary>
    /// <param name="serverId">The ID of the server.</param>
    /// <param name="filter">Optional filter to apply to the player list.</param>
    /// <param name="pageSize">Number of results per page (default: 20).</param>
    /// <param name="pageNumber">Page number for pagination (default: 1).</param>
    /// <returns>A paginated list of players on the server.</returns>
    Task<JsonApiDocument<List<PlayerDTO>>> GetPlayersAsync(
        string serverId,
        string? filter = null,
        int pageSize = 20,
        int pageNumber = 1
    );

    /// <summary>
    /// Gets a specific player by UID from the specified server.
    /// </summary>
    /// <param name="serverId">The ID of the server.</param>
    /// <param name="playerUid">The unique identifier of the player.</param>
    /// <returns>The player information.</returns>
    Task<JsonApiDocument<PlayerDTO>> GetPlayerAsync(string serverId, string playerUid);

    /// <summary>
    /// Gets detailed player information including inventories by UID.
    /// </summary>
    /// <param name="serverId">The ID of the server.</param>
    /// <param name="playerUid">The unique identifier of the player.</param>
    /// <returns>Detailed player information with inventory data.</returns>
    Task<JsonApiDocument<PlayerDetailsDTO>> GetPlayerDetailsAsync(
        string serverId,
        string playerUid
    );

    /// <summary>
    /// Gets player sessions for a specific player on a server.
    /// </summary>
    /// <param name="serverId">The ID of the server.</param>
    /// <param name="playerId">The player UID to get sessions for.</param>
    /// <param name="page">Page number for pagination (default: 1).</param>
    /// <param name="pageSize">Number of results per page (default: 20).</param>
    /// <param name="sorts">Optional sorting specification.</param>
    /// <param name="filters">Optional filters to apply.</param>
    /// <returns>A paginated list of player sessions.</returns>
    Task<JsonApiDocument<List<PlayerSessionDTO>>> GetPlayerSessionsAsync(
        string serverId,
        string playerId,
        int page = 1,
        int pageSize = 20,
        string? sorts = null,
        string? filters = null
    );

    /// <summary>
    /// Kicks a player from the server.
    /// </summary>
    /// <param name="serverId">The ID of the server.</param>
    /// <param name="playerUid">The unique identifier of the player to kick.</param>
    /// <param name="reason">Optional reason for the kick.</param>
    Task<JsonApiDocument<object>> KickPlayerAsync(
        string serverId,
        string playerUid,
        string? reason = null
    );

    /// <summary>
    /// Bans a player from the server.
    /// </summary>
    /// <param name="serverId">The ID of the server.</param>
    /// <param name="playerUid">The unique identifier of the player to ban.</param>
    /// <param name="reason">Optional reason for the ban.</param>
    Task<JsonApiDocument<object>> BanPlayerAsync(
        string serverId,
        string playerUid,
        string? reason = null
    );

    /// <summary>
    /// Adds a player to the server's whitelist.
    /// </summary>
    /// <param name="serverId">The ID of the server.</param>
    /// <param name="playerUid">The unique identifier of the player to whitelist.</param>
    Task<JsonApiDocument<object>> WhitelistPlayerAsync(string serverId, string playerUid);

    /// <summary>
    /// Removes a player from the server's whitelist.
    /// </summary>
    /// <param name="serverId">The ID of the server.</param>
    /// <param name="playerUid">The unique identifier of the player to remove from whitelist.</param>
    Task<JsonApiDocument<object>> RemoveFromWhitelistAsync(string serverId, string playerUid);
}
