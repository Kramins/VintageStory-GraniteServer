using System;

namespace GraniteServer.Api.Models.VintageStory;

public class ServerConfigRole
{
    public string Code { get; set; } = string.Empty;
    public int PrivilegeLevel { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DefaultGameMode { get; set; }
    public string Color { get; set; } = string.Empty;
    public long LandClaimAllowance { get; set; }
    public int LandClaimMaxAreas { get; set; }
    public bool AutoGrant { get; set; }
    public string[] Privileges { get; set; } = Array.Empty<string>();
}
