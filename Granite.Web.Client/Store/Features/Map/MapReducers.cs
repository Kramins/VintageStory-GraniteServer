using System.Collections.Immutable;
using Fluxor;

namespace Granite.Web.Client.Store.Features.Map;

public static class MapReducers
{
    [ReducerMethod]
    public static MapState OnUpdatePlayerMapPosition(
        MapState state,
        UpdatePlayerMapPositionAction action
    )
    {
        var position = new PlayerMapPosition
        {
            PlayerUID = action.PlayerUID,
            BlockX = action.BlockX,
            BlockZ = action.BlockZ,
            PlayerName = action.PlayerName,
            LastUpdated = DateTime.UtcNow
        };

        return state with
        {
            PlayerPositions = state.PlayerPositions.SetItem(action.PlayerUID, position),
            LastUpdated = DateTime.UtcNow,
        };
    }

    [ReducerMethod]
    public static MapState OnRemovePlayerFromMap(MapState state, RemovePlayerFromMapAction action)
    {
        return state with
        {
            PlayerPositions = state.PlayerPositions.Remove(action.PlayerUID),
            LastUpdated = DateTime.UtcNow,
        };
    }

    [ReducerMethod]
    public static MapState OnClearMapPlayers(MapState state, ClearMapPlayersAction action)
    {
        return state with
        {
            PlayerPositions = ImmutableDictionary<string, PlayerMapPosition>.Empty,
            LastUpdated = DateTime.UtcNow,
        };
    }
}
