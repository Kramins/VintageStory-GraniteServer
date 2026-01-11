using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GraniteServer.Api.Models;
using GraniteServer.Api.Models.VintageStory;
using Newtonsoft.Json;
using Vintagestory.API.Server;

namespace GraniteServer.Api.Services;

public class PermissionsService
{
    private readonly ICoreServerAPI _api;

    public PermissionsService(ICoreServerAPI api)
    {
        _api = api;
    }

    public List<PrivilegeDTO> GetAssignablePermissions()
    {
        var privileges = Privilege.AllCodes().Select(p => new PrivilegeDTO { Code = p }).ToList();

        return privileges;
    }

    public List<RoleDTO> GetAssignableRoles()
    {
        var dataDir = _api.DataBasePath ?? string.Empty;
        var configPath = Path.Combine(dataDir, "serverconfig.json");

        var json = File.ReadAllText(configPath);
        var cfg = JsonConvert.DeserializeObject<ServerConfigFile>(json);

        var roles =
            cfg?.Roles.Select(r => new RoleDTO
                {
                    Code = r.Code,
                    PrivilegeLevel = r.PrivilegeLevel,
                    Name = r.Name,
                    Description = r.Description,
                    DefaultGameMode = r.DefaultGameMode,
                    Color = r.Color,
                    LandClaimAllowance = r.LandClaimAllowance,
                    LandClaimMaxAreas = r.LandClaimMaxAreas,
                    AutoGrant = r.AutoGrant,
                    Privileges = r.Privileges,
                })
                .ToList()
            ?? new List<RoleDTO>();

        return roles;
    }

    public List<PlayerGroupDTO> GetAssignablePlayerGroups()
    {
        var groups = _api
            .Groups.PlayerGroupsById.Select(g => new PlayerGroupDTO
            {
                Uid = g.Value.Uid,
                Name = g.Value.Name,
                CreatedDate = g.Value.CreatedDate,
                OwnerUID = g.Value.OwnerUID,
                Md5Identifier = g.Value.Md5Identifier,
                CreatedByPrivateMessage = g.Value.CreatedByPrivateMessage,
                JoinPolicy = g.Value.JoinPolicy,
            })
            .ToList();

        return groups;
    }
}
