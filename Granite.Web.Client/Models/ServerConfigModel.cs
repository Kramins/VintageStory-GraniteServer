namespace Granite.Web.Client.Models;

/// <summary>
/// Mutable model for server configuration form binding.
/// </summary>
public class ServerConfigModel
{
    public int? Port { get; set; }
    public string? ServerName { get; set; }
    public string? WelcomeMessage { get; set; }
    public int? MaxClients { get; set; }
    public string? Password { get; set; }
    public int? MaxChunkRadius { get; set; }
    public bool WhitelistMode { get; set; }
    public bool AllowPvP { get; set; }
    public bool AllowFireSpread { get; set; }
    public bool AllowFallingBlocks { get; set; }
}
