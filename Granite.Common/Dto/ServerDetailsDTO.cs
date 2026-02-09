using System;

namespace Granite.Common.Dto;

public record ServerDetailsDTO
{
    // From ServerDTO
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastSeenAt { get; init; }
    
    // From ServerStatusDTO
    public bool IsOnline { get; init; }
    public int CurrentPlayers { get; init; }
    public int MaxPlayers { get; init; }
    public int UpTime { get; init; }
    public long MemoryUsageBytes { get; init; }
    public string ServerIp { get; init; } = string.Empty;
    public int WorldAgeDays { get; init; }
    public string GameVersion { get; init; } = string.Empty;
    public string WorldName { get; init; } = string.Empty;
    public int WorldSeed { get; init; }
}
