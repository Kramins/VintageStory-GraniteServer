using Granite.Common.Dto.JsonApi;

namespace Granite.Web.Client.Services.Api;

/// <summary>
/// HTTP client for World API.
/// </summary>
public class WorldApiClient : BaseApiClient, IWorldApiClient
{
    private const string BasePath = "/api/world";

    public WorldApiClient(HttpClient httpClient, ILogger<WorldApiClient> logger)
        : base(httpClient, logger)
    {
    }

    public async Task<JsonApiDocument<object>> GetWorldInfoAsync(string serverId)
    {
        try
        {
            return await GetAsync<object>($"{BasePath}/info?serverId={serverId}");
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to fetch world info for server {ServerId}", serverId);
            throw;
        }
    }

    public async Task<JsonApiDocument<string>> GetWorldSeedAsync(string serverId)
    {
        try
        {
            return await GetAsync<string>($"{BasePath}/seed?serverId={serverId}");
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to fetch world seed for server {ServerId}", serverId);
            throw;
        }
    }

    public async Task<JsonApiDocument<object>> SaveWorldAsync(string serverId)
    {
        try
        {
            return await PostAsync<object>($"{BasePath}/save?serverId={serverId}", null);
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to save world for server {ServerId}", serverId);
            throw;
        }
    }

    public async Task<JsonApiDocument<object>> GetWorldMapAsync(string serverId)
    {
        try
        {
            return await GetAsync<object>($"{BasePath}/map?serverId={serverId}");
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to fetch world map for server {ServerId}", serverId);
            throw;
        }
    }

    public async Task<JsonApiDocument<List<object>>> GetCollectiblesAsync(string serverId)
    {
        try
        {
            return await GetAsync<List<object>>($"{BasePath}/collectibles?serverId={serverId}");
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to fetch collectibles for server {ServerId}", serverId);
            throw;
        }
    }
}
