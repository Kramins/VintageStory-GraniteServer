using System.Collections.Immutable;
using Fluxor;

namespace Granite.Web.Client.Store.Features.Players;

public static class PlayersReducers
{
    [ReducerMethod]
    public static PlayersState OnFetchPlayers(PlayersState state, FetchPlayersAction action)
    {
        return state with
        {
            IsLoading = true,
            ErrorMessage = null
        };
    }

    [ReducerMethod]
    public static PlayersState OnFetchPlayersSuccess(PlayersState state, FetchPlayersSuccessAction action)
    {
        return state with
        {
            Players = action.Players.ToImmutableList(),
            IsLoading = false,
            ErrorMessage = null,
            LastUpdated = DateTime.UtcNow
        };
    }

    [ReducerMethod]
    public static PlayersState OnFetchPlayersFailure(PlayersState state, FetchPlayersFailureAction action)
    {
        return state with
        {
            IsLoading = false,
            ErrorMessage = action.ErrorMessage
        };
    }

    [ReducerMethod]
    public static PlayersState OnSelectPlayer(PlayersState state, SelectPlayerAction action)
    {
        return state with
        {
            IsLoading = true,
            ErrorMessage = null
        };
    }

    [ReducerMethod]
    public static PlayersState OnSelectPlayerSuccess(PlayersState state, SelectPlayerSuccessAction action)
    {
        return state with
        {
            SelectedPlayer = action.Player,
            IsLoading = false,
            ErrorMessage = null
        };
    }

    [ReducerMethod]
    public static PlayersState OnSelectPlayerFailure(PlayersState state, SelectPlayerFailureAction action)
    {
        return state with
        {
            IsLoading = false,
            ErrorMessage = action.ErrorMessage
        };
    }

    [ReducerMethod]
    public static PlayersState OnClearPlayersError(PlayersState state, ClearPlayersErrorAction action)
    {
        return state with
        {
            ErrorMessage = null
        };
    }
}
