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
        // var result = await _modManagementService.GetInstalledModsAsync();
        // return new JsonApiDocument<List<ModDTO>>(result);
        return new JsonApiDocument<List<ModDTO>>(new List<ModDTO>());
    }

    [ResourceMethod(GenHTTP.Api.Protocol.RequestMethod.Post)]
    public async Task<JsonApiDocument<string>> InstallModAsync(InstallModRequest request)
    {
        // var result = await _modManagementService.InstallModAsync(request.id);
        // return new JsonApiDocument<string>(result);
        return new JsonApiDocument<string>("Not implemented");
    }
}
