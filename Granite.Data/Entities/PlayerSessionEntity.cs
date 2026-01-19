using System;

namespace GraniteServer.Data.Entities;

public class PlayerSessionEntity
{
    public Guid Id { get; set; }
    /// <summary>
    /// The player this session belongs to from PlayerEntity.Id
    /// </summary>
    public Guid PlayerId { get; set; }
    
    /// <summary>
    /// The server this session belongs to from ServerEntity.Id
    /// </summary>
    public Guid ServerId { get; set; }
    public DateTime JoinDate { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public DateTime? LeaveDate { get; set; }
    public double? Duration { get; set; }

    // Navigation properties
    public PlayerEntity? Player { get; set; }
}
