namespace Granite.Common.Dto;

public record ServerStatusDTO
{
    public string ServerIp { get; init; } = string.Empty;
    public int UpTime { get; init; }
    public int CurrentPlayers { get; init; }
    public int MaxPlayers { get; init; }
    public string ServerName { get; init; } = string.Empty;
    public int WorldAgeDays { get; init; }
    public long MemoryUsageBytes { get; init; }
    public bool IsOnline { get; init; }
    public string GameVersion { get; init; } = string.Empty;
    public string WorldName { get; init; } = string.Empty;
    public int WorldSeed { get; init; }
}
