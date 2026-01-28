using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;

namespace Granite.Web.Client.Services.Api;

/// <summary>
/// HTTP client for Players API.
/// </summary>
public class PlayersApiClient : BaseApiClient, IPlayersApiClient
{
    private const string BasePath = "/api/players";

    public PlayersApiClient(HttpClient httpClient, ILogger<PlayersApiClient> logger)
        : base(httpClient, logger)
    {
    }

    public async Task<JsonApiDocument<List<PlayerDTO>>> GetPlayersAsync(string? filter = null, int pageSize = 20, int pageNumber = 1)
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
            return await GetAsync<List<PlayerDTO>>(url);
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to fetch players");
            throw;
        }
    }

    public async Task<JsonApiDocument<PlayerDTO>> GetPlayerAsync(string playerUid)
    {
        try
        {
            return await GetAsync<PlayerDTO>($"{BasePath}/{playerUid}");
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to fetch player {PlayerUid}", playerUid);
            throw;
        }
    }

    public async Task<JsonApiDocument<PlayerDTO>> UpdatePlayerAsync(string playerUid, PlayerDetailsDTO playerData)
    {
        try
        {
            return await PutAsync<PlayerDTO>($"{BasePath}/{playerUid}", playerData);
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to update player {PlayerUid}", playerUid);
            throw;
        }
    }

    public async Task<JsonApiDocument<object>> KickPlayerAsync(string playerUid, string? reason = null)
    {
        try
        {
            var request = new { reason };
            return await PostAsync<object>($"{BasePath}/{playerUid}/kick", request);
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to kick player {PlayerUid}", playerUid);
            throw;
        }
    }

    public async Task<JsonApiDocument<object>> BanPlayerAsync(string playerUid, string? reason = null)
    {
        try
        {
            var request = new { reason };
            return await PostAsync<object>($"{BasePath}/{playerUid}/ban", request);
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to ban player {PlayerUid}", playerUid);
            throw;
        }
    }

    public async Task<JsonApiDocument<object>> WhitelistPlayerAsync(string playerUid)
    {
        try
        {
            return await PostAsync<object>($"{BasePath}/{playerUid}/whitelist", null);
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to whitelist player {PlayerUid}", playerUid);
            throw;
        }
    }

    public async Task<JsonApiDocument<object>> RemoveFromWhitelistAsync(string playerUid)
    {
        try
        {
            // For DELETE operations, we need to handle them specially since DeleteAsync doesn't return data
            var response = await HttpClient.DeleteAsync($"{BasePath}/{playerUid}/whitelist");
            if (!response.IsSuccessStatusCode)
            {
                throw new ApiException($"Failed to remove player from whitelist: {response.StatusCode}");
            }
            return new JsonApiDocument<object> { Data = null };
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to remove player from whitelist {PlayerUid}", playerUid);
            throw;
        }
    }
}
