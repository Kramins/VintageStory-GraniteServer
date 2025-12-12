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
            Id = p.PlayerUID,
            Name = p.PlayerName,
            IpAddress = p.IpAddress,
            LanguageCode = p.LanguageCode,
            Ping = p.Ping,
            RolesCode = p.Role.Code,
            FirstJoinDate = p.ServerData.FirstJoinDate,
            LastJoinDate = p.ServerData.LastJoinDate,
            Privileges = p.Privileges.ToArray(),
            IsAdmin = false
        }).ToList();
    }


    [ControllerAction(RequestMethod.Post)]
    public void Kick(KickRequestDTO request)
    {
        var player = _api.Server.Players.Where(p => p.PlayerUID == request.PlayerId).FirstOrDefault();
        if (player == null)
        {
            // Find way to return 404
            return;
        }
        var reason = string.IsNullOrWhiteSpace(request.Reason) ? "Kicked by an administrator." : request.Reason;
        try
        {
            player.Disconnect(reason);
        }
        catch (Exception ex)
        {
            // Do nothing for now
            // Possible bug in VS where disconnecting a player immediately after they join causes an exception
        }
    }

    public record WhitelistRequestDTO(string PlayerName);
    [ControllerAction(RequestMethod.Post)]
    public void AddToWhitelist(WhitelistRequestDTO request)
    {
        return;

    }


}
