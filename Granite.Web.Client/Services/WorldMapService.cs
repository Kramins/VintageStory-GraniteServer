using System.Net.Http.Json;
using Granite.Common.Dto;
using Microsoft.Extensions.Logging;

namespace Granite.Web.Client.Services;

public class WorldMapService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WorldMapService> _logger;

    public WorldMapService(IHttpClientFactory httpClientFactory, ILogger<WorldMapService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<WorldMapBoundsDTO?> GetWorldBoundsAsync(Guid serverId)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("GraniteApi");
            var response = await httpClient.GetAsync($"api/worldmap/{serverId}/bounds");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get world bounds for server {ServerId}: {StatusCode}", 
                    serverId, response.StatusCode);
                return null;
            }

            var jsonApiResponse = await response.Content.ReadFromJsonAsync<Common.Dto.JsonApi.JsonApiDocument<WorldMapBoundsDTO>>();
            return jsonApiResponse?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting world bounds for server {ServerId}", serverId);
            return null;
        }
    }

    public async Task<MapTileMetadataDTO?> GetTileMetadataAsync(Guid serverId, int x, int y)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("GraniteApi");
            var response = await httpClient.GetAsync($"api/worldmap/{serverId}/tiles/{x}/{y}/metadata");
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var jsonApiResponse = await response.Content.ReadFromJsonAsync<Common.Dto.JsonApi.JsonApiDocument<MapTileMetadataDTO>>();
            return jsonApiResponse?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tile metadata for server {ServerId} at ({X}, {Y})", serverId, x, y);
            return null;
        }
    }

    public string GetTileImageUrl(Guid serverId, int x, int y)
    {
        var httpClient = _httpClientFactory.CreateClient("GraniteApi");
        var baseAddress = httpClient.BaseAddress;
        return $"{baseAddress}api/worldmap/{serverId}/tiles/grouped/{x}/{y}";
    }
}
