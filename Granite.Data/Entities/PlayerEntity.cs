using System;

namespace GraniteServer.Data.Entities;

public class PlayerEntity
{
    /// <summary>
    /// The internal database ID, unique per server
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The Vintagestory Player UID, the same across all servers
    /// </summary>
    public string PlayerUID { get; set; } = string.Empty;

    /// <summary>
    /// The server this player belongs to
    /// </summary>
    public Guid ServerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime FirstJoinDate { get; set; }
    public DateTime LastJoinDate { get; set; }
    public bool IsWhitelisted { get; set; } = false;
    public string? WhitelistedReason { get; set; }
    public string? WhitelistedBy { get; set; }
    public bool IsBanned { get; set; } = false;
    public string? BanReason { get; set; }
    public string? BanBy { get; set; }
    public DateTime? BanUntil { get; set; }

    // Navigation properties
    public ServerEntity? Server { get; set; }
    public ICollection<PlayerSessionEntity> Sessions { get; set; } = new List<PlayerSessionEntity>();
}
