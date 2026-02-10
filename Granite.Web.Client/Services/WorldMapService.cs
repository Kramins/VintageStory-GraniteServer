using System.Net.Http.Json;
using System.Reactive.Linq;
using Granite.Common.Dto;
using Granite.Common.Messaging.Events.Client;
using Granite.Web.Client.Services.Auth;
using Microsoft.Extensions.Logging;

namespace Granite.Web.Client.Services;

public class WorldMapService : IDisposable
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ClientMessageBusService _messageBus;
    private readonly CustomAuthenticationStateProvider _authProvider;
    private readonly ILogger<WorldMapService> _logger;

    private List<IDisposable> _subscriptions = new();

    public WorldMapService(
        IHttpClientFactory httpClientFactory,
        CustomAuthenticationStateProvider authenticationStateProvider,
        ClientMessageBusService messageBus,
        ILogger<WorldMapService> logger
    )
    {
        _httpClientFactory = httpClientFactory;
        _messageBus = messageBus;
        _authProvider = authenticationStateProvider;
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
                _logger.LogWarning(
                    "Failed to get world bounds for server {ServerId}: {StatusCode}",
                    serverId,
                    response.StatusCode
                );
                return null;
            }

            var jsonApiResponse =
                await response.Content.ReadFromJsonAsync<Common.Dto.JsonApi.JsonApiDocument<WorldMapBoundsDTO>>();
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
            var response = await httpClient.GetAsync(
                $"api/worldmap/{serverId}/tiles/{x}/{y}/metadata"
            );

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var jsonApiResponse =
                await response.Content.ReadFromJsonAsync<Common.Dto.JsonApi.JsonApiDocument<MapTileMetadataDTO>>();
            return jsonApiResponse?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting tile metadata for server {ServerId} at ({X}, {Y})",
                serverId,
                x,
                y
            );
            return null;
        }
    }

    public object SubscribeToTileUpdates(Guid serverId, Action<MapTilesUpdatedEvent> onUpdate)
    {
        var subscription = _messageBus
            .GetObservable<MapTilesUpdatedEvent>(serverId)
            .Throttle(TimeSpan.FromMilliseconds(5000)) // Throttle to avoid flooding
            .Subscribe(onUpdate);

        _subscriptions.Add(subscription);
        return subscription;
    }

    public async Task<string> GetTileServerBearerTokenAsync()
    {
        var token = await _authProvider.GetTokenAsync();
        return token ?? string.Empty;
    }

    public string GetBaseTileUrl(Guid serverId)
    {
        var httpClient = _httpClientFactory.CreateClient("GraniteApi");
        var baseAddress = httpClient.BaseAddress;
        return $"{baseAddress}api/worldmap/{serverId}/tiles/grouped";
    }

    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }
    }
}
