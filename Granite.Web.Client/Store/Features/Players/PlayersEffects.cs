using Fluxor;
using Granite.Web.Client.Services.Api;
using Microsoft.Extensions.Logging;

namespace Granite.Web.Client.Store.Features.Players;

public class PlayersEffects
{
    private readonly IServerPlayersApiClient _playersApiClient;
    private readonly ILogger<PlayersEffects> _logger;
    private readonly IState<PlayersState> _playersState;

    public PlayersEffects(
        IServerPlayersApiClient playersApiClient,
        ILogger<PlayersEffects> logger,
        IState<PlayersState> playersState
    )
    {
        _playersApiClient = playersApiClient;
        _logger = logger;
        _playersState = playersState;
    }

    [EffectMethod]
    public async Task HandleFetchPlayersAction(FetchPlayersAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Fetching players for server {ServerId}", action.ServerId);

            var response = await _playersApiClient.GetPlayersAsync(
                action.ServerId,
                filter: null,
                pageSize: 100,
                pageNumber: 1
            );

            if (response?.Data != null)
            {
                dispatcher.Dispatch(new FetchPlayersSuccessAction(response.Data, action.ServerId));
            }
            else
            {
                dispatcher.Dispatch(
                    new FetchPlayersSuccessAction(
                        new List<Granite.Common.Dto.PlayerDTO>(),
                        action.ServerId
                    )
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch players for server {ServerId}", action.ServerId);
            dispatcher.Dispatch(new FetchPlayersFailureAction(ex.Message));
        }
    }

    [EffectMethod]
    public Task HandleLoadPlayersIfNeededAction(
        LoadPlayersIfNeededAction action,
        IDispatcher dispatcher
    )
    {
        var state = _playersState.Value;

        // Check if we already have players loaded for this server
        if (state.CurrentServerId == action.ServerId && state.Players.Any() && !state.IsLoading)
        {
            _logger.LogInformation(
                "Players already loaded for server {ServerId}, skipping fetch",
                action.ServerId
            );
            return Task.CompletedTask;
        }

        // Need to fetch - dispatch the actual fetch action
        _logger.LogInformation(
            "Players not loaded for server {ServerId}, fetching...",
            action.ServerId
        );
        dispatcher.Dispatch(new FetchPlayersAction(action.ServerId));
        return Task.CompletedTask;
    }

    [EffectMethod]
    public Task HandleRefreshPlayersAction(RefreshPlayersAction action, IDispatcher dispatcher)
    {
        _logger.LogInformation("Forcing refresh of players for server {ServerId}", action.ServerId);
        dispatcher.Dispatch(new FetchPlayersAction(action.ServerId));
        return Task.CompletedTask;
    }

    [EffectMethod]
    public async Task HandleFetchPlayerDetailsAction(
        FetchPlayerDetailsAction action,
        IDispatcher dispatcher
    )
    {
        try
        {
            _logger.LogInformation(
                "Fetching player details for player {PlayerUid} on server {ServerId}",
                action.PlayerUid,
                action.ServerId
            );

            var response = await _playersApiClient.GetPlayerDetailsAsync(
                action.ServerId,
                action.PlayerUid
            );

            if (response?.Data != null)
            {
                dispatcher.Dispatch(new FetchPlayerDetailsSuccessAction(response.Data));
            }
            else
            {
                dispatcher.Dispatch(
                    new FetchPlayerDetailsFailureAction("Player details not found")
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to fetch player details for {PlayerUid} on server {ServerId}",
                action.PlayerUid,
                action.ServerId
            );
            dispatcher.Dispatch(new FetchPlayerDetailsFailureAction(ex.Message));
        }
    }
}
