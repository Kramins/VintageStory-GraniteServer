using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GenHTTP.Api.Protocol;
using GenHTTP.Modules.Webservices;
using GraniteServer.Api.Models;
using GraniteServer.Api.Services;

namespace GraniteServer.Api.Controllers;

public class WorldController
{
    private WorldService _worldService;

    public WorldController(WorldService worldService)
    {
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));
    }

    [ResourceMethod(RequestMethod.Get, "/collectibles")]
    public Task<List<CollectibleObjectDTO>> GetAllCollectiblesAsync()
    {
        return _worldService.GetAllCollectiblesAsync();
    }
}
