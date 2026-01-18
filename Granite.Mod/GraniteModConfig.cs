using System;

namespace GraniteServer.Mod;

public class GraniteModConfig
{
    public Guid ServerId { get; set; } = Guid.NewGuid();
    public string GraniteServerHost { get; set; } = "http://localhost:5000";
    public string HubPath { get; set; } = "/hub/mod";
    public string? AccessToken { get; set; } = null;

    // reconnect delays in seconds; default (immediate, 2s, 10s, 30s)
    public int[] ReconnectDelaysSeconds { get; set; } = new[] { 0, 2, 10, 30 };
}
