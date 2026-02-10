using Fluxor;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Granite.Web.Client.Store.Features.Map;

public class MapEffects
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<MapEffects> _logger;

    public MapEffects(IJSRuntime jsRuntime, ILogger<MapEffects> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    [EffectMethod]
    public async Task HandleRemovePlayerFromMapAction(
        RemovePlayerFromMapAction action,
        IDispatcher dispatcher
    )
    {
        try
        {
            _logger.LogInformation(
                "Removing player marker from map: {PlayerUID}",
                action.PlayerUID
            );

            await _jsRuntime.InvokeVoidAsync("mapInterop.removePlayerMarker", action.PlayerUID);

            _logger.LogDebug("Player marker removed successfully: {PlayerUID}", action.PlayerUID);
        }
        catch (JSException jsEx)
        {
            _logger.LogError(
                jsEx,
                "JavaScript error while removing player marker {PlayerUID}: {Message}",
                action.PlayerUID,
                jsEx.Message
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to remove player marker {PlayerUID}",
                action.PlayerUID
            );
        }
    }
}
