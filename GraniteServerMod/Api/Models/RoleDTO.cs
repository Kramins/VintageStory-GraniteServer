using System;

namespace GraniteServer.Api.Models;

public class RoleDTO
{
    public string Code { get; set; }
    public int PrivilegeLevel { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int DefaultGameMode { get; set; }
    public string Color { get; set; }
    public long LandClaimAllowance { get; set; }
    public int LandClaimMaxAreas { get; set; }
    public bool AutoGrant { get; set; }
    public string[] Privileges { get; set; }
}
