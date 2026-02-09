using Fluxor;
using Granite.Common.Dto;

namespace Granite.Web.Client.Store.Features.Server;

public static class ServerReducers
{
    [ReducerMethod]
    public static ServerState ReduceFetchServersAction(ServerState state, FetchServersAction action)
    {
        return state with { IsLoading = true, Error = null };
    }

    [ReducerMethod]
    public static ServerState ReduceFetchServersSuccessAction(ServerState state, FetchServersSuccessAction action)
    {
        // Auto-select first server if none selected
        var selectedServerId = state.SelectedServerId ?? action.Servers.FirstOrDefault()?.Id.ToString();
        return state with
        {
            Servers = action.Servers,
            SelectedServerId = selectedServerId,
            IsLoading = false,
            Error = null
        };
    }

    [ReducerMethod]
    public static ServerState ReduceFetchServersFailureAction(ServerState state, FetchServersFailureAction action)
    {
        return state with { IsLoading = false, Error = action.Error };
    }

    [ReducerMethod]
    public static ServerState ReduceSelectServerAction(ServerState state, SelectServerAction action)
    {
        return state with { SelectedServerId = action.ServerId };
    }

    [ReducerMethod]
    public static ServerState ReduceCreateServerAction(ServerState state, CreateServerAction action)
    {
        return state with { IsLoading = true, Error = null };
    }

    [ReducerMethod]
    public static ServerState ReduceCreateServerSuccessAction(ServerState state, CreateServerSuccessAction action)
    {
        var updatedServers = new List<ServerDetailsDTO>(state.Servers) { action.Server };
        return state with
        {
            Servers = updatedServers,
            IsLoading = false,
            Error = null
        };
    }

    [ReducerMethod]
    public static ServerState ReduceCreateServerFailureAction(ServerState state, CreateServerFailureAction action)
    {
        return state with { IsLoading = false, Error = action.Error };
    }

    [ReducerMethod]
    public static ServerState ReduceUpdateServerAction(ServerState state, UpdateServerAction action)
    {
        return state with { IsLoading = true, Error = null };
    }

    [ReducerMethod]
    public static ServerState ReduceUpdateServerSuccessAction(ServerState state, UpdateServerSuccessAction action)
    {
        var updatedServers = state.Servers.Select(s =>
            s.Id == action.Server.Id ? action.Server : s
        ).ToList();
        return state with
        {
            Servers = updatedServers,
            IsLoading = false,
            Error = null
        };
    }

    [ReducerMethod]
    public static ServerState ReduceUpdateServerFailureAction(ServerState state, UpdateServerFailureAction action)
    {
        return state with { IsLoading = false, Error = action.Error };
    }

    [ReducerMethod]
    public static ServerState ReduceDeleteServerAction(ServerState state, DeleteServerAction action)
    {
        return state with { IsLoading = true, Error = null };
    }

    [ReducerMethod]
    public static ServerState ReduceDeleteServerSuccessAction(ServerState state, DeleteServerSuccessAction action)
    {
        var updatedServers = state.Servers.Where(s => s.Id != action.ServerId).ToList();
        return state with
        {
            Servers = updatedServers,
            IsLoading = false,
            Error = null
        };
    }

    [ReducerMethod]
    public static ServerState ReduceDeleteServerFailureAction(ServerState state, DeleteServerFailureAction action)
    {
        return state with { IsLoading = false, Error = action.Error };
    }

    [ReducerMethod]
    public static ServerState ReduceClearServerErrorAction(ServerState state, ClearServerErrorAction action)
    {
        return state with { Error = null };
    }
}
