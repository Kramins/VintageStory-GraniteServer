using System;
using System.Linq;
using GenHTTP.Api.Protocol;
using GenHTTP.Modules.Controllers;
using GenHTTP.Modules.Webservices;
using GraniteServer.Api.Models;
using Vintagestory.API.Server;

namespace GraniteServer.Api;

/// <summary>
/// Server lifecycle and control controller
/// </summary>
public class ServerController
{
    private readonly ICoreServerAPI _api;

    public ServerController(ICoreServerAPI api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    /// <summary>
    /// Announce a message to all players
    /// </summary>
    [ResourceMethod(RequestMethod.Post, "/announce/")]
    public object Announce(string message)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Reload server configuration
    /// </summary>
    [ResourceMethod(RequestMethod.Post, "/reload/")]
    public object ReloadConfiguration()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Trigger an immediate autosave
    /// </summary>
    [ResourceMethod(RequestMethod.Post, "/save/")]
    public object SaveNow()
    {
        throw new NotImplementedException();
    }

    [ResourceMethod(RequestMethod.Get, "/status/")]
    public ServerStatusDTO Status()
    {
        var response = new ServerStatusDTO
        {
            ServerIp = _api.Server.ServerIp,
            UpTime = _api.Server.ServerUptimeSeconds,
            CurrentPlayers = _api.Server.Players.Count(p =>
                p.ConnectionState == EnumClientState.Connected
            ),
            MaxPlayers = _api.Server.Config.MaxClients,
            ServerName = _api.Server.Config.ServerName,
            GameVersion = "na",
            WorldAgeDays = (int)_api.World.Calendar.TotalDays,
            WorldName = _api.World.WorldName,
            WorldSeed = _api.World.Seed,
            MemoryUsageBytes = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64,
            IsOnline = _api.Server != null,
        };

        return response;
    }

    /// <summary>
    /// Stop the server
    /// </summary>
    [ResourceMethod(RequestMethod.Post, "/stop/")]
    public object StopServer(int? exitCode = null)
    {
        throw new NotImplementedException();
    }

    [ResourceMethod(RequestMethod.Post, "/whitelist-mode/")]
    public void setWhitelistMode(SetWhitelistModeRequestDTO request)
    {
        _api.Server.Config.WhitelistMode = request.Enabled
            ? EnumWhitelistMode.On
            : EnumWhitelistMode.Off;
    }

    public record SetWhitelistModeRequestDTO(bool Enabled);
}
