namespace Granite.Common.Dto;

public record ServerConfigDTO
{
    public int? Port { get; init; }
    public string? ServerName { get; init; }
    public string? WelcomeMessage { get; init; }
    public int? MaxClients { get; init; }
    public string? Password { get; init; }
    public int? MaxChunkRadius { get; init; }
    public bool? WhitelistMode { get; init; }
    public bool? AllowPvP { get; init; }
    public bool? AllowFireSpread { get; init; }
    public bool? AllowFallingBlocks { get; init; }
    public string? AccessToken { get; init; }
}
