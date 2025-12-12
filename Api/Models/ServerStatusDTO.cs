namespace GraniteServer.Api.Models;

public class ServerStatusDTO
{
    public string ServerIp { get; internal set; }
    public int UpTime { get; internal set; }
    public int CurrentPlayers { get; internal set; }
    public int MaxPlayers { get; internal set; }
    public string ServerName { get; internal set; }
    public int WorldAgeDays { get; internal set; }
    public long MemoryUsageBytes { get; internal set; }
    public bool IsOnline { get; internal set; }
    public string GameVersion { get; internal set; }
    public string WorldName { get; internal set; }
    public int WorldSeed { get; internal set; }
}
