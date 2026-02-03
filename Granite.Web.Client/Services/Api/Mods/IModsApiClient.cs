using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;

namespace Granite.Web.Client.Services.Api;

/// <summary>
/// Interface for Mods API client.
/// </summary>
public interface IModsApiClient
{
    /// <summary>
    /// Gets all available mods.
    /// </summary>
    Task<JsonApiDocument<List<ModDTO>>> GetModsAsync(string? filter = null, int pageSize = 20, int pageNumber = 1);

    /// <summary>
    /// Gets a specific mod by ID.
    /// </summary>
    Task<JsonApiDocument<ModDTO>> GetModAsync(string modId);

    /// <summary>
    /// Installs a mod on the server.
    /// </summary>
    Task<JsonApiDocument<object>> InstallModAsync(string modId);

    /// <summary>
    /// Uninstalls a mod from the server.
    /// </summary>
    Task<JsonApiDocument<object>> UninstallModAsync(string modId);

    /// <summary>
    /// Gets the status of mod installation/uninstallation.
    /// </summary>
    Task<JsonApiDocument<object>> GetModStatusAsync(string modId);
}
