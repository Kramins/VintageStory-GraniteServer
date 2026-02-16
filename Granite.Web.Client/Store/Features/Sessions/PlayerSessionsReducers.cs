using System.Collections.Immutable;
using Fluxor;

namespace Granite.Web.Client.Store.Features.Sessions;

public static class PlayerSessionsReducers
{
    [ReducerMethod]
    public static PlayerSessionsState ReduceLoadPlayerSessionsAction(PlayerSessionsState state, LoadPlayerSessionsAction action)
    {
        return state with
        {
            IsLoading = true,
            ErrorMessage = null,
            CurrentServerId = action.ServerId,
            CurrentPlayerId = action.PlayerId,
            CurrentPage = action.Page,
            PageSize = action.PageSize
        };
    }

    [ReducerMethod]
    public static PlayerSessionsState ReduceLoadPlayerSessionsSuccessAction(
        PlayerSessionsState state,
        LoadPlayerSessionsSuccessAction action
    )
    {
        return state with
        {
            Sessions = action.Sessions.ToImmutableList(),
            TotalItems = action.TotalItems,
            CurrentPage = action.Page,
            IsLoading = false,
            ErrorMessage = null,
            CurrentServerId = action.ServerId,
            CurrentPlayerId = action.PlayerId
        };
    }

    [ReducerMethod]
    public static PlayerSessionsState ReduceLoadPlayerSessionsFailureAction(
        PlayerSessionsState state,
        LoadPlayerSessionsFailureAction action
    )
    {
        return state with
        {
            IsLoading = false,
            ErrorMessage = action.ErrorMessage
        };
    }

    [ReducerMethod]
    public static PlayerSessionsState ReduceClearPlayerSessionsAction(PlayerSessionsState state, ClearPlayerSessionsAction action)
    {
        return new PlayerSessionsState();
    }
}
