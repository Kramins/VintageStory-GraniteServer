using System;

namespace GraniteServer.Api.Models;

public class ModDTO
{
    public Guid ModId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Author { get; set; }
    public string? RunningVersion { get; internal set; }
    public string? InstalledVersion { get; internal set; }
}
