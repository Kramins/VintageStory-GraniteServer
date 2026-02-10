namespace Granite.Web.Client.Store.Features.Map;

/// <summary>
/// Update a player's position on the map.
/// </summary>
public record UpdatePlayerMapPositionAction(
    string PlayerUID,
    float BlockX,
    float BlockZ,
    string PlayerName
);

/// <summary>
/// Remove a player from the map (when they disconnect).
/// </summary>
public record RemovePlayerFromMapAction(string PlayerUID);

/// <summary>
/// Clear all player positions from the map.
/// </summary>
public record ClearMapPlayersAction();
