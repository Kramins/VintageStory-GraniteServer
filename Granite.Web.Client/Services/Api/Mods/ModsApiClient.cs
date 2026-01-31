using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;

namespace Granite.Web.Client.Services.Api;

/// <summary>
/// HTTP client for Mods API.
/// </summary>
public class ModsApiClient : BaseApiClient, IModsApiClient
{
    private const string BasePath = "/api/mods";

    public ModsApiClient(IHttpClientFactory httpClientFactory, ILogger<ModsApiClient> logger)
        : base(httpClientFactory, logger)
    {
    }

    public async Task<JsonApiDocument<List<ModDTO>>> GetModsAsync(string? filter = null, int pageSize = 20, int pageNumber = 1)
    {
        var url = BasePath;
        var queryParams = new List<string>();

        if (!string.IsNullOrEmpty(filter))
        {
            queryParams.Add($"filters={Uri.EscapeDataString(filter)}");
        }

        queryParams.Add($"pageSize={pageSize}");
        queryParams.Add($"pageNumber={pageNumber}");

        if (queryParams.Any())
        {
            url += "?" + string.Join("&", queryParams);
        }

        try
        {
            return await GetAsync<List<ModDTO>>(url);
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to fetch mods");
            throw;
        }
    }

    public async Task<JsonApiDocument<ModDTO>> GetModAsync(string modId)
    {
        try
        {
            return await GetAsync<ModDTO>($"{BasePath}/{modId}");
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to fetch mod {ModId}", modId);
            throw;
        }
    }

    public async Task<JsonApiDocument<object>> InstallModAsync(string modId)
    {
        try
        {
            var request = new InstallModRequest { ModId = modId };
            return await PostAsync<object>($"{BasePath}/{modId}/install", request);
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to install mod {ModId}", modId);
            throw;
        }
    }

    public async Task<JsonApiDocument<object>> UninstallModAsync(string modId)
    {
        try
        {
            var httpClient = GetHttpClient();
            var response = await httpClient.DeleteAsync($"{BasePath}/{modId}");
            if (!response.IsSuccessStatusCode)
            {
                throw new ApiException($"Failed to uninstall mod: {response.StatusCode}");
            }
            return new JsonApiDocument<object> { Data = null };
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to uninstall mod {ModId}", modId);
            throw;
        }
    }

    public async Task<JsonApiDocument<object>> GetModStatusAsync(string modId)
    {
        try
        {
            return await GetAsync<object>($"{BasePath}/{modId}/status");
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to fetch mod status {ModId}", modId);
            throw;
        }
    }
}
