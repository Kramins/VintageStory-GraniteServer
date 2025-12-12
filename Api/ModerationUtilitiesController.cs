using System;
using GenHTTP.Modules.Controllers;
using GenHTTP.Modules.Webservices;

namespace GraniteServer.Api;

/// <summary>
/// Moderation and utility command controller
/// </summary>
public class ModerationUtilitiesController
{
    /// <summary>
    /// Manage server whitelist
    /// Linked to: /whitelist command
    /// </summary>
    public object ManageWhitelist(string action, string? playerName = null)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Manage IP block list
    /// Linked to: /ipblock command (clears automatically every 10 minutes)
    /// </summary>
    public object ManageIpBlockList(string action, string? ipAddress = null)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Get list of banned players
    /// Linked to: /list command (banned players list)
    /// </summary>
    public object ListBannedPlayers()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Execute debug utilities and developer tools
    /// Linked to: /debug and /dev commands
    /// </summary>
    public object ExecuteDebugCommand(string command)
    {
        throw new NotImplementedException();
    }
}
