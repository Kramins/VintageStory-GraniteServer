using System;

namespace Granite.Common.Dto;

public record ModDTO
{
    public Guid ModId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string? RunningVersion { get; init; }
    public string? InstalledVersion { get; init; }
}
