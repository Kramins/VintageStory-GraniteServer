using System;
using System.Collections.Generic;
using GenHTTP.Api.Protocol;
using GenHTTP.Modules.Controllers;
using GenHTTP.Modules.Reflection;
using GenHTTP.Modules.Webservices;
using GraniteServer.Api.Models;
using GraniteServer.Api.Models.JsonApi;
using GraniteServer.Api.Services;
using Vintagestory.API.Server;

namespace GraniteServer.Api;

public class PermissionsController
{
    private readonly PermissionsService _service;
    private readonly ICoreServerAPI _api;

    public PermissionsController(PermissionsService service, ICoreServerAPI api)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    [ResourceMethod(RequestMethod.Get, "/privileges")]
    public JsonApiDocument<List<PrivilegeDTO>> GetAssignablePermissions()
    {
        try
        {
            var data = _service.GetAssignablePermissions();
            return new JsonApiDocument<List<PrivilegeDTO>>(data);
        }
        catch (Exception ex)
        {
            _api.Logger.Warning("Error retrieving assignable privileges: " + ex.Message);
            throw;
        }
    }

    [ResourceMethod(RequestMethod.Get, "/roles")]
    public JsonApiDocument<List<RoleDTO>> GetAssignableRoles()
    {
        try
        {
            var data = _service.GetAssignableRoles();
            return new JsonApiDocument<List<RoleDTO>>(data);
        }
        catch (Exception ex)
        {
            _api.Logger.Warning("Error retrieving assignable roles: " + ex.Message);
            throw;
        }
    }

    [ResourceMethod(RequestMethod.Get, "/groups")]
    public JsonApiDocument<List<PlayerGroupDTO>> GetAssignablePlayerGroups()
    {
        try
        {
            var data = _service.GetAssignablePlayerGroups();
            return new JsonApiDocument<List<PlayerGroupDTO>>(data);
        }
        catch (Exception ex)
        {
            _api.Logger.Warning("Error retrieving assignable player groups: " + ex.Message);
            throw;
        }
    }
}
