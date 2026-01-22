using System;

namespace Granite.Common.Dto;

public class ModDTO
{
    public Guid ModId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? RunningVersion { get; set; }
    public string? InstalledVersion { get; set; }
}
