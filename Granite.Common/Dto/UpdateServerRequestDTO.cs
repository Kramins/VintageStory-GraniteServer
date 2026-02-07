namespace Granite.Common.Dto;

public class UpdateServerRequestDTO
{
    public required string Name { get; set; }
    public string? Description { get; set; }

    // Optional server configuration
    public int? Port { get; set; }
    public string? WelcomeMessage { get; set; }
    public int? MaxClients { get; set; }
    public string? Password { get; set; }
    public int? MaxChunkRadius { get; set; }
    public bool? WhitelistMode { get; set; }
    public bool? AllowPvP { get; set; }
    public bool? AllowFireSpread { get; set; }
    public bool? AllowFallingBlocks { get; set; }
}
