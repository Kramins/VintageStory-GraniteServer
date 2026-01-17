using System;

namespace GraniteServer.Data.Entities;

public class PlayerSessionEntity
{
    public Guid Id { get; set; }
    public string PlayerId { get; set; }
    public Guid ServerId { get; set; }
    public DateTime JoinDate { get; set; }
    public string IpAddress { get; set; }
    public string PlayerName { get; set; }
    public DateTime? LeaveDate { get; set; }
    public double? Duration { get; set; }
}
