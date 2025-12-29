using System.Collections.Generic;
using System.Threading.Tasks;
using GenHTTP.Modules.Webservices;
using GraniteServer.Api.Models.JsonApi;
using GraniteServerMod.Api.Models;
using GraniteServerMod.Api.Services;

namespace GraniteServerMod.Api.Controllers;

public class ModManagementController
{
    private readonly ModManagementService _modManagementService;

    public ModManagementController(ModManagementService modManagementService)
    {
        _modManagementService = modManagementService;
    }

    [ResourceMethod(GenHTTP.Api.Protocol.RequestMethod.Get)]
    public async Task<JsonApiDocument<List<ModDTO>>> GetModsAsync()
    {
        var result = await _modManagementService.GetInstalledModsAsync();
        return new JsonApiDocument<List<ModDTO>>(result);
    }
}
