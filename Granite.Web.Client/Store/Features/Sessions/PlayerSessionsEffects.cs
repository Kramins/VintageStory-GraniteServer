using Fluxor;
using Granite.Web.Client.Services.Api;
using Microsoft.Extensions.Logging;

namespace Granite.Web.Client.Store.Features.Sessions;

public class PlayerSessionsEffects
{
    private readonly IServerPlayersApiClient _playersApiClient;
    private readonly ILogger<PlayerSessionsEffects> _logger;

    public PlayerSessionsEffects(
        IServerPlayersApiClient playersApiClient,
        ILogger<PlayerSessionsEffects> logger
    )
    {
        _playersApiClient = playersApiClient;
        _logger = logger;
    }

    [EffectMethod]
    public async Task HandleLoadPlayerSessionsAction(LoadPlayerSessionsAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation(
                "Loading sessions for server {ServerId}, player {PlayerId}, page {Page}",
                action.ServerId,
                action.PlayerId,
                action.Page
            );

            var response = await _playersApiClient.GetPlayerSessionsAsync(
                action.ServerId,
                action.PlayerId,
                action.Page,
                action.PageSize,
                action.Sorts,
                action.Filters
            );

            if (response?.Data != null)
            {
                var totalItems = response.Meta?.Pagination?.TotalCount ?? 0;
                
                dispatcher.Dispatch(
                    new LoadPlayerSessionsSuccessAction(
                        response.Data,
                        totalItems,
                        action.Page,
                        action.ServerId,
                        action.PlayerId
                    )
                );
            }
            else
            {
                dispatcher.Dispatch(
                    new LoadPlayerSessionsSuccessAction(
                        new List<Granite.Common.Dto.PlayerSessionDTO>(),
                        0,
                        action.Page,
                        action.ServerId,
                        action.PlayerId
                    )
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to load sessions for server {ServerId}", 
                action.ServerId
            );
            dispatcher.Dispatch(new LoadPlayerSessionsFailureAction(ex.Message));
        }
    }
}
