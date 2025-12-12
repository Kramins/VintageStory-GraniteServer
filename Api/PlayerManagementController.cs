using System;
using System.Collections.Generic;
using System.Linq;
using GenHTTP.Api.Protocol;
using GenHTTP.Modules.Controllers;
using GenHTTP.Modules.Reflection;
using GenHTTP.Modules.Webservices;
using GraniteServer.Api.Models;
using Vintagestory.API.Server;

namespace GraniteServer.Api;

/// <summary>
/// Player control and administration controller
/// </summary>
public class PlayerManagementController
{
    private readonly ICoreServerAPI _api;

    public PlayerManagementController(ICoreServerAPI api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    /// <summary>
    /// List all connected players
    /// </summary>
    [ControllerAction(GenHTTP.Api.Protocol.RequestMethod.Get)]
    public IList<PlayerDTO> ListPlayers()
    {
        var allServerPlayers = _api.Server.Players.ToList();
        return allServerPlayers.Select(p => new PlayerDTO
        {
            Name = p.PlayerName,
            Id = p.PlayerUID,
            IsAdmin = false
        }).ToList();
    }

    public record WhitelistRequestDTO(string PlayerName);
    [ControllerAction(RequestMethod.Post)]
    public void AddToWhitelist(WhitelistRequestDTO request)
    {
        return;

    }


    /// <summary>
    /// Kick a player from the server
    /// </summary>
    [ControllerAction(GenHTTP.Api.Protocol.RequestMethod.Post)]
    public object KickPlayer(string playerName, string? reason = null)
    {
        throw new NotImplementedException();
    }


    /// <summary>
    /// Ban a player from the server
    /// </summary>
    [ControllerAction(GenHTTP.Api.Protocol.RequestMethod.Post)]
    public object BanPlayer(string playerName, string duration, string reason)
    {
        throw new NotImplementedException();
    }


    /// <summary>
    /// Remove a player ban
    /// </summary>
    [ControllerAction(GenHTTP.Api.Protocol.RequestMethod.Delete)]
    public object UnbanPlayer(string playerName)
    {
        throw new NotImplementedException();
    }


    /// <summary>
    /// Give a player admin status
    /// </summary>
    [ControllerAction(GenHTTP.Api.Protocol.RequestMethod.Post)]
    public object PromoteToOp(string playerName)
    {
        throw new NotImplementedException();
    }


    /// <summary>
    /// Set a player's game mode
    /// </summary>
    [ControllerAction(GenHTTP.Api.Protocol.RequestMethod.Put)]
    public object SetGameMode(string playerName, string mode)
    {
        throw new NotImplementedException();
    }
}
