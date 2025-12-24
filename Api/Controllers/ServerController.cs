using System;
using System.Linq;
using System.Threading.Tasks;
using GenHTTP.Api.Protocol;
using GenHTTP.Engine.Internal;
using GenHTTP.Modules.Controllers;
using GenHTTP.Modules.Webservices;
using GraniteServer.Api.Models;
using GraniteServer.Api.Services;
using Vintagestory.API.Server;

namespace GraniteServer.Api;

/// <summary>
/// Server lifecycle and control controller
/// </summary>
public class ServerController
{
    private readonly ICoreServerAPI _api;
    private readonly ServerService _serverService;

    public ServerController(ICoreServerAPI api, ServerService serverService)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _serverService = serverService ?? throw new ArgumentNullException(nameof(serverService));
    }

    /// <summary>
    /// Announce a message to all players
    /// </summary>
    [ResourceMethod(RequestMethod.Post, "/announce/")]
    public object Announce(string message)
    {
        throw new NotImplementedException();
    }

    [ResourceMethod(RequestMethod.Get, "/status")]
    public ServerStatusDTO Status()
    {
        var response = new ServerStatusDTO
        {
            ServerIp = _api.Server.ServerIp,
            UpTime = _api.Server.ServerUptimeSeconds,
            CurrentPlayers = _api.Server.Players.Count(p =>
                p.ConnectionState == EnumClientState.Playing
            ),
            MaxPlayers = _api.Server.Config.MaxClients,
            ServerName = _api.Server.Config.ServerName,
            GameVersion = "NA",
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

    [ResourceMethod(RequestMethod.Get, "/config")]
    public async Task<ServerConfigDTO> GetServerConfigAsync()
    {
        return await _serverService.GetServerConfig();
    }

    [ResourceMethod(RequestMethod.Post, "/config")]
    public async Task UpdateConfigAsync(ServerConfigDTO config)
    {
        await _serverService.UpdateConfigAsync(config);
    }
}
