using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;

namespace Granite.Web.Client.Services.Api;

/// <summary>
/// Interface for Server API client.
/// </summary>
public interface IServerApiClient
{
    /// <summary>
    /// Gets all servers.
    /// </summary>
    Task<JsonApiDocument<List<ServerDetailsDTO>>> GetServersAsync();

    /// <summary>
    /// Gets a specific server by ID.
    /// </summary>
    Task<JsonApiDocument<ServerDetailsDTO>> GetServerAsync(string serverId);

    /// <summary>
    /// Creates a new server.
    /// </summary>
    Task<JsonApiDocument<ServerDTO>> CreateServerAsync(CreateServerRequestDTO request);

    /// <summary>
    /// Updates an existing server.
    /// </summary>
    Task<JsonApiDocument<ServerDTO>> UpdateServerAsync(string serverId, UpdateServerRequestDTO request);

    /// <summary>
    /// Deletes a server.
    /// </summary>
    Task<JsonApiDocument<object>> DeleteServerAsync(string serverId);

    /// <summary>
    /// Regenerates the access token for a server.
    /// </summary>
    Task<JsonApiDocument<TokenRegeneratedResponseDTO>> RegenerateAccessTokenAsync(string serverId);

    /// <summary>
    /// Gets the server status (now returns ServerDetailsDTO).
    /// </summary>
    Task<JsonApiDocument<ServerDetailsDTO>> GetServerStatusAsync(string serverId);

    /// <summary>
    /// Gets the server configuration.
    /// </summary>
    Task<JsonApiDocument<ServerConfigDTO>> GetServerConfigAsync(string serverId);

    /// <summary>
    /// Updates the server configuration.
    /// </summary>
    Task<JsonApiDocument<ServerConfigDTO>> UpdateServerConfigAsync(string serverId, ServerConfigDTO config);

    /// <summary>
    /// Restarts the server.
    /// </summary>
    Task<JsonApiDocument<object>> RestartServerAsync(string serverId);

    /// <summary>
    /// Stops the server.
    /// </summary>
    Task<JsonApiDocument<object>> StopServerAsync(string serverId);

    /// <summary>
    /// Gets the health status of the server.
    /// </summary>
    Task<JsonApiDocument<HealthDTO>> GetHealthAsync(string serverId);
}
