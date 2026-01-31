using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;

namespace Granite.Web.Client.Services.Api;

/// <summary>
/// HTTP client for Players API.
/// </summary>
public class PlayersApiClient : BaseApiClient, IPlayersApiClient
{
    public PlayersApiClient(IHttpClientFactory httpClientFactory, ILogger<PlayersApiClient> logger)
        : base(httpClientFactory, logger)
    {
    }

    private string GetBasePath(string serverId) => $"/api/{serverId}/players";

    public async Task<JsonApiDocument<List<PlayerDTO>>> GetPlayersAsync(string serverId, string? filter = null, int pageSize = 20, int pageNumber = 1)
    {
        var url = GetBasePath(serverId);
        var queryParams = new List<string>();

        if (!string.IsNullOrEmpty(filter))
        {
            queryParams.Add($"filters={Uri.EscapeDataString(filter)}");
        }

        queryParams.Add($"page={pageNumber}");
        queryParams.Add($"pageSize={pageSize}");

        if (queryParams.Any())
        {
            url += "?" + string.Join("&", queryParams);
        }

        try
        {
            return await GetAsync<List<PlayerDTO>>(url);
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to fetch players for server {ServerId}", serverId);
            throw;
        }
    }

    public async Task<JsonApiDocument<PlayerDTO>> GetPlayerAsync(string serverId, string playerUid)
    {
        try
        {
            return await GetAsync<PlayerDTO>($"{GetBasePath(serverId)}/{playerUid}");
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to fetch player {PlayerUid} for server {ServerId}", playerUid, serverId);
            throw;
        }
    }

    public async Task<JsonApiDocument<PlayerDetailsDTO>> GetPlayerDetailsAsync(string serverId, string playerUid)
    {
        try
        {
            return await GetAsync<PlayerDetailsDTO>($"{GetBasePath(serverId)}/{playerUid}");
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to fetch player details for {PlayerUid} for server {ServerId}", playerUid, serverId);
            throw;
        }
    }

    public async Task<JsonApiDocument<PlayerDTO>> UpdatePlayerAsync(string serverId, string playerUid, PlayerDetailsDTO playerData)
    {
        try
        {
            return await PutAsync<PlayerDTO>($"{GetBasePath(serverId)}/{playerUid}", playerData);
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to update player {PlayerUid} for server {ServerId}", playerUid, serverId);
            throw;
        }
    }

    public async Task<JsonApiDocument<object>> KickPlayerAsync(string serverId, string playerUid, string? reason = null)
    {
        try
        {
            var request = new { reason };
            return await PostAsync<object>($"{GetBasePath(serverId)}/{playerUid}/kick", request);
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to kick player {PlayerUid} for server {ServerId}", playerUid, serverId);
            throw;
        }
    }

    public async Task<JsonApiDocument<object>> BanPlayerAsync(string serverId, string playerUid, string? reason = null)
    {
        try
        {
            var request = new { reason };
            return await PostAsync<object>($"{GetBasePath(serverId)}/{playerUid}/ban", request);
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to ban player {PlayerUid} for server {ServerId}", playerUid, serverId);
            throw;
        }
    }

    public async Task<JsonApiDocument<object>> WhitelistPlayerAsync(string serverId, string playerUid)
    {
        try
        {
            return await PostAsync<object>($"{GetBasePath(serverId)}/{playerUid}/whitelist", null);
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to whitelist player {PlayerUid} for server {ServerId}", playerUid, serverId);
            throw;
        }
    }

    public async Task<JsonApiDocument<object>> RemoveFromWhitelistAsync(string serverId, string playerUid)
    {
        try
        {
            var httpClient = GetHttpClient();
            var response = await httpClient.DeleteAsync($"{GetBasePath(serverId)}/{playerUid}/whitelist");
            if (!response.IsSuccessStatusCode)
            {
                throw new ApiException($"Failed to remove player from whitelist: {response.StatusCode}");
            }
            return new JsonApiDocument<object> { Data = null };
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to remove player from whitelist {PlayerUid} for server {ServerId}", playerUid, serverId);
            throw;
        }
    }
}
