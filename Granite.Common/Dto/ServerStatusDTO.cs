namespace Granite.Common.Dto;

public class ServerStatusDTO
{
    public string ServerIp { get; set; } = string.Empty;
    public int UpTime { get; set; }
    public int CurrentPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public string ServerName { get; set; } = string.Empty;
    public int WorldAgeDays { get; set; }
    public long MemoryUsageBytes { get; set; }
    public bool IsOnline { get; set; }
    public string GameVersion { get; set; } = string.Empty;
    public string WorldName { get; set; } = string.Empty;
    public int WorldSeed { get; set; }
}
