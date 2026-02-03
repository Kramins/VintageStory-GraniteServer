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
    Task<JsonApiDocument<List<ServerDTO>>> GetServersAsync();

    /// <summary>
    /// Gets a specific server by ID.
    /// </summary>
    Task<JsonApiDocument<ServerDTO>> GetServerAsync(string serverId);

    /// <summary>
    /// Gets the server status.
    /// </summary>
    Task<JsonApiDocument<ServerStatusDTO>> GetServerStatusAsync(string serverId);

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
