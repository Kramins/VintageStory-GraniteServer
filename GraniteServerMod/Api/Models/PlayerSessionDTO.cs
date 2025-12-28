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

    /// <summary>
    /// Duration of the session in minutes. Calculated as (LeaveDate - JoinDate) or null if still active.
    /// </summary>
    public int? DurationMinutes
    {
        get
        {
            if (LeaveDate.HasValue)
            {
                return (int)(LeaveDate.Value - JoinDate).TotalMinutes;
            }
            return null;
        }
    }

    /// <summary>
    /// Indicates if the session is currently active (no LeaveDate).
    /// </summary>
    public bool IsActive => !LeaveDate.HasValue;
}
