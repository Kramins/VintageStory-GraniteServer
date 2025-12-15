using System;
using GenHTTP.Modules.Controllers;
using GenHTTP.Modules.Webservices;

namespace GraniteServer.Api;

/// <summary>
/// Group and privilege management controller
/// </summary>
public class GroupPermissionController
{
    /// <summary>
    /// Add a player to a group
    /// Linked to: /group command
    /// </summary>
    public object AddPlayerToGroup(string groupName, string playerName)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Create a new player group
    /// Linked to: /group command
    /// </summary>
    public object CreateGroup(string groupName)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// List all player groups
    /// Linked to: /group and /list command
    /// </summary>
    public object ListGroups()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// List all player roles
    /// Linked to: /role <rolename> and /list command
    /// </summary>
    public object ListRoles()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Remove a player from a group
    /// Linked to: /group command
    /// </summary>
    public object RemovePlayerFromGroup(string groupName, string playerName)
    {
        throw new NotImplementedException();
    }
}
