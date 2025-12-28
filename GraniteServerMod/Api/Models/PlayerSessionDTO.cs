using System;

namespace GraniteServer.Api.Models;

public class PlayerSessionDTO
{
    public Guid Id { get; set; }
    public string PlayerId { get; set; } = string.Empty;
    public Guid ServerId { get; set; }
    public string ServerName { get; set; } = string.Empty;
    public DateTime JoinDate { get; set; }
    public DateTime? LeaveDate { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;

    public double? Duration { get; set; }

    public bool IsActive { get; set; }
}
