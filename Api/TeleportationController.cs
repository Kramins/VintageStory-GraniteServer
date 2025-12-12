using System;
using GenHTTP.Modules.Controllers;
using GenHTTP.Modules.Webservices;

namespace GraniteServer.Api;

/// <summary>
/// Player teleportation and waypoint controller
/// </summary>
public class TeleportationController
{
    /// <summary>
    /// Teleport a player to a location
    /// Linked to: /tp <source> <target> command
    /// </summary>
    public object TeleportPlayer(string playerName, string targetPosition)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Teleport a player to a waypoint
    /// Linked to: /tpwp <name> command
    /// </summary>
    public object TeleportToWaypoint(string playerName, string waypointName)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Teleport to a story structure
    /// Linked to: /tpstoryloc <resonancearchive/lazaret/village/devastationarea/tobiascave/treasurehunter> command
    /// </summary>
    public object TeleportToStoryLocation(string playerName, string locationCode)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// List all waypoints
    /// Linked to: /waypoint command
    /// </summary>
    public object ListWaypoints()
    {
        throw new NotImplementedException();
    }
}
