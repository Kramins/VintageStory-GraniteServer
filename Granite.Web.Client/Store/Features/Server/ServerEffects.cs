using Fluxor;
using Granite.Web.Client.Services.Api;
using Granite.Web.Client.Store.Features.Players;

namespace Granite.Web.Client.Store.Features.Server;

public class ServerEffects
{
    private readonly IServerApiClient _serverApiClient;
    private readonly ILogger<ServerEffects> _logger;

    public ServerEffects(IServerApiClient serverApiClient, ILogger<ServerEffects> logger)
    {
        _serverApiClient = serverApiClient;
        _logger = logger;
    }

    [EffectMethod]
    public async Task HandleFetchServersAction(FetchServersAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Fetching servers...");
            var response = await _serverApiClient.GetServersAsync();

            if (response?.Data != null)
            {
                _logger.LogInformation("Fetched {Count} servers", response.Data.Count);
                dispatcher.Dispatch(new FetchServersSuccessAction(response.Data));
            }
            else
            {
                _logger.LogWarning("Server list response was null or empty");
                dispatcher.Dispatch(new FetchServersSuccessAction([]));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch servers");
            dispatcher.Dispatch(new FetchServersFailureAction(ex.Message));
        }
    }

    [EffectMethod]
    public Task HandleSelectServerAction(SelectServerAction action, IDispatcher dispatcher)
    {
        _logger.LogInformation("Server changed to {ServerId}, clearing player state", action.ServerId);
        dispatcher.Dispatch(new ClearPlayersAction());
        return Task.CompletedTask;
    }
}
