using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;

namespace Granite.Web.Client.Services.Api;

/// <summary>
/// HTTP client for Server API.
/// </summary>
public class ServerApiClient : BaseApiClient, IServerApiClient
{
    private const string BasePath = "/api/servers";

    public ServerApiClient(IHttpClientFactory httpClientFactory, ILogger<ServerApiClient> logger)
        : base(httpClientFactory, logger)
    {
    }

    public async Task<JsonApiDocument<List<ServerDetailsDTO>>> GetServersAsync()
    {
        try
        {
            return await GetAsync<List<ServerDetailsDTO>>(BasePath);
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to fetch servers");
            throw;
        }
    }

    public async Task<JsonApiDocument<ServerDetailsDTO>> GetServerAsync(string serverId)
    {
        try
        {
            return await GetAsync<ServerDetailsDTO>($"{BasePath}/{serverId}");
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to fetch server {ServerId}", serverId);
            throw;
        }
    }

    public async Task<JsonApiDocument<ServerDTO>> CreateServerAsync(CreateServerRequestDTO request)
    {
        try
        {
            return await PostAsync<ServerDTO>(BasePath, request);
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to create server");
            throw;
        }
    }

    public async Task<JsonApiDocument<ServerDTO>> UpdateServerAsync(string serverId, UpdateServerRequestDTO request)
    {
        try
        {
            return await PutAsync<ServerDTO>($"{BasePath}/{serverId}", request);
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to update server {ServerId}", serverId);
            throw;
        }
    }

    public async Task<JsonApiDocument<object>> DeleteServerAsync(string serverId)
    {
        try
        {
            await DeleteAsync($"{BasePath}/{serverId}");
            return new JsonApiDocument<object>();
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to delete server {ServerId}", serverId);
            throw;
        }
    }

    public async Task<JsonApiDocument<TokenRegeneratedResponseDTO>> RegenerateAccessTokenAsync(string serverId)
    {
        try
        {
            return await PostAsync<TokenRegeneratedResponseDTO>($"{BasePath}/{serverId}/regenerate-token", null);
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to regenerate token {ServerId}", serverId);
            throw;
        }
    }

    public async Task<JsonApiDocument<ServerDetailsDTO>> GetServerStatusAsync(string serverId)
    {
        try
        {
            return await GetAsync<ServerDetailsDTO>($"/api/{serverId}/server/status");
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
            return await GetAsync<ServerConfigDTO>($"/api/{serverId}/config");
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
            return await PatchAsync<ServerConfigDTO>($"/api/{serverId}/config", config);
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
