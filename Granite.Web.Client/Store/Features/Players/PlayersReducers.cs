using System.Collections.Immutable;
using Fluxor;
using Granite.Common.Dto;

namespace Granite.Web.Client.Store.Features.Players;

public static class PlayersReducers
{
    [ReducerMethod]
    public static PlayersState OnFetchPlayers(PlayersState state, FetchPlayersAction action)
    {
        return state with { IsLoading = true, ErrorMessage = null };
    }

    [ReducerMethod]
    public static PlayersState OnFetchPlayersSuccess(
        PlayersState state,
        FetchPlayersSuccessAction action
    )
    {
        return state with
        {
            Players = action.Players.ToImmutableList(),
            IsLoading = false,
            ErrorMessage = null,
            LastUpdated = DateTime.UtcNow,
            CurrentServerId = action.ServerId,
        };
    }

    [ReducerMethod]
    public static PlayersState OnFetchPlayersFailure(
        PlayersState state,
        FetchPlayersFailureAction action
    )
    {
        return state with { IsLoading = false, ErrorMessage = action.ErrorMessage };
    }

    [ReducerMethod]
    public static PlayersState OnSelectPlayer(PlayersState state, SelectPlayerAction action)
    {
        return state with { IsLoading = true, ErrorMessage = null };
    }

    [ReducerMethod]
    public static PlayersState OnSelectPlayerSuccess(
        PlayersState state,
        SelectPlayerSuccessAction action
    )
    {
        return state with
        {
            SelectedPlayer = action.Player,
            IsLoading = false,
            ErrorMessage = null,
        };
    }

    [ReducerMethod]
    public static PlayersState OnSelectPlayerFailure(
        PlayersState state,
        SelectPlayerFailureAction action
    )
    {
        return state with { IsLoading = false, ErrorMessage = action.ErrorMessage };
    }

    [ReducerMethod]
    public static PlayersState OnClearPlayersError(
        PlayersState state,
        ClearPlayersErrorAction action
    )
    {
        return state with { ErrorMessage = null };
    }

    [ReducerMethod]
    public static PlayersState OnLoadPlayersIfNeeded(
        PlayersState state,
        LoadPlayersIfNeededAction action
    )
    {
        // Don't change state here - effect will decide if fetch is needed
        return state;
    }

    [ReducerMethod]
    public static PlayersState OnRefreshPlayers(PlayersState state, RefreshPlayersAction action)
    {
        return state with { IsLoading = true, ErrorMessage = null };
    }

    [ReducerMethod]
    public static PlayersState OnClearPlayers(PlayersState state, ClearPlayersAction action)
    {
        return new PlayersState();
    }

    [ReducerMethod]
    public static PlayersState OnUpdatePlayerConnectionState(
        PlayersState state,
        UpdatePlayerConnectionStateAction action
    )
    {
        var players = state.Players;
        PlayerDTO? updatedPlayer = null;

        var index = players.FindIndex(p =>
            p.PlayerUID == action.PlayerUID && p.ServerId == action.ServerId
        );

        if (index >= 0)
        {
            var existing = players[index];

            updatedPlayer = existing with
            {
                ConnectionState = action.ConnectionState,
                IpAddress = !string.IsNullOrEmpty(action.IpAddress)
                    ? action.IpAddress
                    : existing.IpAddress,
            };

            players = players.SetItem(index, updatedPlayer);
        }
        else if (!string.IsNullOrEmpty(action.Name))
        {
            updatedPlayer = new PlayerDTO
            {
                PlayerUID = action.PlayerUID,
                ServerId = action.ServerId,
                Name = action.Name,
                ConnectionState = action.ConnectionState,
                IpAddress = action.IpAddress ?? string.Empty,
            };

            players = players.Add(updatedPlayer);
        }

        var selected = state.SelectedPlayerDetails;

        if (
            selected != null
            && selected.PlayerUID == action.PlayerUID
            && selected.ServerId == action.ServerId
        )
        {
            selected = selected with
            {
                ConnectionState = action.ConnectionState,
                IpAddress = action.IpAddress ?? selected.IpAddress,
            };
        }

        if (players == state.Players && selected == state.SelectedPlayerDetails)
            return state;

        return state with
        {
            Players = players,
            SelectedPlayerDetails = selected,
        };
    }

    [ReducerMethod]
    public static PlayersState OnUpdatePlayerBanStatus(
        PlayersState state,
        UpdatePlayerBanStatusAction action
    )
    {
        var index = state.Players.FindIndex(p =>
            p.PlayerUID == action.PlayerUID && p.ServerId == action.ServerId
        );

        if (index >= 0)
        {
            var existingPlayer = state.Players[index];
            var updatedPlayer = existingPlayer with
            {
                IsBanned = action.IsBanned,
                BanReason = action.BanReason
            };
            return state with { Players = state.Players.SetItem(index, updatedPlayer) };
        }

        return state;
    }

    [ReducerMethod]
    public static PlayersState OnUpdatePlayerWhitelistStatus(
        PlayersState state,
        UpdatePlayerWhitelistStatusAction action
    )
    {
        var index = state.Players.FindIndex(p =>
            p.PlayerUID == action.PlayerUID && p.ServerId == action.ServerId
        );

        if (index >= 0)
        {
            var existingPlayer = state.Players[index];
            var updatedPlayer = existingPlayer with { IsWhitelisted = action.IsWhitelisted };
            return state with { Players = state.Players.SetItem(index, updatedPlayer) };
        }

        return state;
    }

    [ReducerMethod]
    public static PlayersState OnFetchPlayerDetails(
        PlayersState state,
        FetchPlayerDetailsAction action
    )
    {
        return state with { IsLoading = true, ErrorMessage = null };
    }

    [ReducerMethod]
    public static PlayersState OnFetchPlayerDetailsSuccess(
        PlayersState state,
        FetchPlayerDetailsSuccessAction action
    )
    {
        return state with
        {
            SelectedPlayerDetails = action.PlayerDetails,
            IsLoading = false,
            ErrorMessage = null,
        };
    }

    [ReducerMethod]
    public static PlayersState OnFetchPlayerDetailsFailure(
        PlayersState state,
        FetchPlayerDetailsFailureAction action
    )
    {
        return state with { IsLoading = false, ErrorMessage = action.ErrorMessage };
    }
}
