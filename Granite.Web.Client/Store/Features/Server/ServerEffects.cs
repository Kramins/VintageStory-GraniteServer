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

    [EffectMethod]
    public async Task HandleCreateServerAction(CreateServerAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Creating server: {Name}", action.Request.Name);
            var response = await _serverApiClient.CreateServerAsync(action.Request);

            if (response?.Data != null)
            {
                _logger.LogInformation("Server created successfully: {ServerId}", response.Data.Id);
                // Refetch the full servers list to get complete details
                dispatcher.Dispatch(new FetchServersAction());
            }
            else
            {
                dispatcher.Dispatch(new CreateServerFailureAction("Failed to create server"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create server");
            dispatcher.Dispatch(new CreateServerFailureAction(ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleUpdateServerAction(UpdateServerAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Updating server: {ServerId}", action.ServerId);
            var response = await _serverApiClient.UpdateServerAsync(action.ServerId.ToString(), action.Request);

            if (response?.Data != null)
            {
                _logger.LogInformation("Server updated successfully: {ServerId}", action.ServerId);
                // Refetch the full servers list to get complete details
                dispatcher.Dispatch(new FetchServersAction());
            }
            else
            {
                dispatcher.Dispatch(new UpdateServerFailureAction("Failed to update server"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update server");
            dispatcher.Dispatch(new UpdateServerFailureAction(ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleDeleteServerAction(DeleteServerAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Deleting server: {ServerId}", action.ServerId);
            await _serverApiClient.DeleteServerAsync(action.ServerId.ToString());

            _logger.LogInformation("Server deleted successfully: {ServerId}", action.ServerId);
            dispatcher.Dispatch(new DeleteServerSuccessAction(action.ServerId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete server");
            dispatcher.Dispatch(new DeleteServerFailureAction(ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleRegenerateTokenAction(RegenerateTokenAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Regenerating token for server: {ServerId}", action.ServerId);
            var response = await _serverApiClient.RegenerateAccessTokenAsync(action.ServerId.ToString());

            if (response?.Data != null)
            {
                _logger.LogInformation("Token regenerated successfully: {ServerId}", action.ServerId);
                dispatcher.Dispatch(new RegenerateTokenSuccessAction(response.Data));
            }
            else
            {
                dispatcher.Dispatch(new RegenerateTokenFailureAction("Failed to regenerate token"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to regenerate token");
            dispatcher.Dispatch(new RegenerateTokenFailureAction(ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleFetchServerConfigAction(FetchServerConfigAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Fetching server config: {ServerId}", action.ServerId);
            var response = await _serverApiClient.GetServerConfigAsync(action.ServerId.ToString());

            if (response?.Data != null)
            {
                _logger.LogInformation("Server config fetched: {ServerId}", action.ServerId);
                dispatcher.Dispatch(new FetchServerConfigSuccessAction(response.Data));
            }
            else
            {
                dispatcher.Dispatch(new FetchServerConfigFailureAction("Failed to fetch server config"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch server config");
            dispatcher.Dispatch(new FetchServerConfigFailureAction(ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleUpdateServerConfigAction(UpdateServerConfigAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Updating server config: {ServerId}", action.ServerId);
            var response = await _serverApiClient.UpdateServerConfigAsync(action.ServerId.ToString(), action.Config);

            if (response?.Data != null)
            {
                _logger.LogInformation("Server config updated: {ServerId}", action.ServerId);
                dispatcher.Dispatch(new UpdateServerConfigSuccessAction(response.Data));
            }
            else
            {
                dispatcher.Dispatch(new UpdateServerConfigFailureAction("Failed to update server config"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update server config");
            dispatcher.Dispatch(new UpdateServerConfigFailureAction(ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleRestartServerAction(RestartServerAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Restarting server: {ServerId}", action.ServerId);
            await _serverApiClient.RestartServerAsync(action.ServerId.ToString());

            _logger.LogInformation("Server restart initiated: {ServerId}", action.ServerId);
            dispatcher.Dispatch(new RestartServerSuccessAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restart server");
            dispatcher.Dispatch(new RestartServerFailureAction(ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleStopServerAction(StopServerAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Stopping server: {ServerId}", action.ServerId);
            await _serverApiClient.StopServerAsync(action.ServerId.ToString());

            _logger.LogInformation("Server stop initiated: {ServerId}", action.ServerId);
            dispatcher.Dispatch(new StopServerSuccessAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop server");
            dispatcher.Dispatch(new StopServerFailureAction(ex.Message));
        }
    }
}
