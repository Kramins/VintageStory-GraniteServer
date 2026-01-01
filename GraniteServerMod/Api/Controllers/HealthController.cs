using System;
using GenHTTP.Modules.Controllers;
using GenHTTP.Modules.Webservices;
using GraniteServer.Api.Models;
using Vintagestory.API.Server;

namespace GraniteServer.Api;

public class HealthDTO
{
    public string Status { get; set; } = "ok";
    public DateTime UtcNow { get; set; } = DateTime.UtcNow;
}

public class HealthController
{
    private readonly ICoreServerAPI _api;

    public HealthController(ICoreServerAPI api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    [ResourceMethod(GenHTTP.Api.Protocol.RequestMethod.Get, "/")]
    public HealthDTO Get()
    {
        return new HealthDTO { Status = "ok", UtcNow = DateTime.UtcNow };
    }
}
