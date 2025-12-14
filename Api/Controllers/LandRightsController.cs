using System;
using GenHTTP.Modules.Controllers;
using GenHTTP.Modules.Webservices;

namespace GraniteServer.Api;

/// <summary>
/// Land claim and block reinforcement controller
/// </summary>
public class LandRightsController
{
    /// <summary>
    /// List all land claims
    /// Linked to: /land command
    /// </summary>
    public object ListLandClaims()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Create a new land claim
    /// Linked to: /land command
    /// </summary>
    public object CreateLandClaim(string position)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Manage privileges for a land claim
    /// Linked to: /land command and /bre, /gbre commands
    /// </summary>
    public object ManageLandPrivileges(string position, string playerName, string privilege)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Manage block reinforcement privileges
    /// Linked to: /bre (player owned) and /gbre (group owned) commands
    /// </summary>
    public object ManageBlockReinforcement(string position, string owner, string action)
    {
        throw new NotImplementedException();
    }
}
