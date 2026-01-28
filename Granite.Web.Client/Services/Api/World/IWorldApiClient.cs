using Granite.Common.Dto.JsonApi;

namespace Granite.Web.Client.Services.Api;

/// <summary>
/// Interface for World API client.
/// </summary>
public interface IWorldApiClient
{
    /// <summary>
    /// Gets information about the server world.
    /// </summary>
    Task<JsonApiDocument<object>> GetWorldInfoAsync(string serverId);

    /// <summary>
    /// Gets the world seed.
    /// </summary>
    Task<JsonApiDocument<string>> GetWorldSeedAsync(string serverId);

    /// <summary>
    /// Saves the world.
    /// </summary>
    Task<JsonApiDocument<object>> SaveWorldAsync(string serverId);

    /// <summary>
    /// Gets the world map data.
    /// </summary>
    Task<JsonApiDocument<object>> GetWorldMapAsync(string serverId);

    /// <summary>
    /// Gets collectible objects in the world.
    /// </summary>
    Task<JsonApiDocument<List<object>>> GetCollectiblesAsync(string serverId);
}
