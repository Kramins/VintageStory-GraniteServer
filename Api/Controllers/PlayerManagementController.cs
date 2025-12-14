using System;
using System.Collections.Generic;
using System.Linq;
using GenHTTP.Api.Protocol;
using GenHTTP.Modules.Controllers;
using GenHTTP.Modules.Reflection;
using GenHTTP.Modules.Webservices;
using GraniteServer.Api.Models;
using GraniteServer.Api.Services;
using Vintagestory.API.Server;

namespace GraniteServer.Api;

/// <summary>
/// Player control and administration controller
/// </summary>
public class PlayerManagementController
{
    private readonly PlayerService _playerService;

    public PlayerManagementController(PlayerService playerService)
    {
        _playerService = playerService ?? throw new ArgumentNullException(nameof(playerService));
    }

    /// <summary>
    /// List all connected players
    /// </summary>
    [ControllerAction(GenHTTP.Api.Protocol.RequestMethod.Get)]
    public IList<PlayerDTO> ListPlayers()
    {
        return _playerService.GetAllPlayers();
    }


    [ControllerAction(RequestMethod.Post)]
    public void Kick(KickRequestDTO request)
    {
        _playerService.KickPlayer(request.PlayerId, request.Reason ?? "Kicked by an administrator.");
    }

    public record WhitelistRequestDTO(string PlayerName);
    [ControllerAction(RequestMethod.Post)]
    public void AddToWhitelist(WhitelistRequestDTO request)
    {
        return;

    }


}
