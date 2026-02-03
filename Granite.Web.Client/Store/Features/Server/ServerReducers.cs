using Fluxor;

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
}
