using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;

namespace Granite.Web.Client.Services.Api;

/// <summary>
/// HTTP client for Server API.
/// </summary>
public class ServerApiClient : BaseApiClient, IServerApiClient
{
    private const string BasePath = "/api/servers";

    public ServerApiClient(HttpClient httpClient, ILogger<ServerApiClient> logger)
        : base(httpClient, logger)
    {
    }

    public async Task<JsonApiDocument<List<ServerDTO>>> GetServersAsync()
    {
        try
        {
            return await GetAsync<List<ServerDTO>>(BasePath);
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to fetch servers");
            throw;
        }
    }

    public async Task<JsonApiDocument<ServerDTO>> GetServerAsync(string serverId)
    {
        try
        {
            return await GetAsync<ServerDTO>($"{BasePath}/{serverId}");
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to fetch server {ServerId}", serverId);
            throw;
        }
    }

    public async Task<JsonApiDocument<ServerStatusDTO>> GetServerStatusAsync(string serverId)
    {
        try
        {
            return await GetAsync<ServerStatusDTO>($"{BasePath}/{serverId}/status");
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to fetch server status {ServerId}", serverId);
            throw;
        }
    }

    public async Task<JsonApiDocument<ServerConfigDTO>> GetServerConfigAsync(string serverId)
    {
        try
        {
            return await GetAsync<ServerConfigDTO>($"{BasePath}/{serverId}/config");
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to fetch server config {ServerId}", serverId);
            throw;
        }
    }

    public async Task<JsonApiDocument<ServerConfigDTO>> UpdateServerConfigAsync(string serverId, ServerConfigDTO config)
    {
        try
        {
            return await PutAsync<ServerConfigDTO>($"{BasePath}/{serverId}/config", config);
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to update server config {ServerId}", serverId);
            throw;
        }
    }

    public async Task<JsonApiDocument<object>> RestartServerAsync(string serverId)
    {
        try
        {
            return await PostAsync<object>($"{BasePath}/{serverId}/restart", null);
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to restart server {ServerId}", serverId);
            throw;
        }
    }

    public async Task<JsonApiDocument<object>> StopServerAsync(string serverId)
    {
        try
        {
            return await PostAsync<object>($"{BasePath}/{serverId}/stop", null);
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to stop server {ServerId}", serverId);
            throw;
        }
    }

    public async Task<JsonApiDocument<HealthDTO>> GetHealthAsync(string serverId)
    {
        try
        {
            return await GetAsync<HealthDTO>($"{BasePath}/{serverId}/health");
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to fetch server health {ServerId}", serverId);
            throw;
        }
    }
}
