using System.Collections.Generic;
using System.Threading.Tasks;
using GenHTTP.Modules.Webservices;
using GraniteServer.Api.Models;
using GraniteServer.Api.Models.JsonApi;
using GraniteServer.Api.Services;

namespace GraniteServer.Api.Controllers;

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
        var result = _modManagementService.GetServerMods();
        return new JsonApiDocument<List<ModDTO>>(result);
    }

    [ResourceMethod(GenHTTP.Api.Protocol.RequestMethod.Post)]
    public async Task<JsonApiDocument<string>> InstallModAsync(InstallModRequest request)
    {
        return new JsonApiDocument<string>("Not implemented");
    }
}
