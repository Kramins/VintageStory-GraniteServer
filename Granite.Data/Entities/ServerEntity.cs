using System;

namespace GraniteServer.Data.Entities;

public class ServerEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSeenAt { get; set; }
    public bool IsOnline { get; set; } = false;

    // Server Configuration
    public int? Port { get; set; }
    public string? WelcomeMessage { get; set; }
    public int? MaxClients { get; set; }
    public string? Password { get; set; }
    public int? MaxChunkRadius { get; set; }
    public bool? WhitelistMode { get; set; }
    public bool? AllowPvP { get; set; }
    public bool? AllowFireSpread { get; set; }
    public bool? AllowFallingBlocks { get; set; }

    // Navigation properties
    public ICollection<PlayerEntity> Players { get; set; } = new List<PlayerEntity>();
    public ICollection<ModServerEntity> ModServers { get; set; } = new List<ModServerEntity>();
    public ICollection<ServerMetricsEntity> ServerMetrics { get; set; } = new List<ServerMetricsEntity>();
    public ICollection<CollectibleEntity> Collectibles { get; set; } = new List<CollectibleEntity>();
}
