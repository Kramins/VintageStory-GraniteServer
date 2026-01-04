using System;
using System.Diagnostics;

namespace GraniteServer.Api.Models.Events;

public class PlayerEventData
{
    public string PlayerName { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public DateTime TimeStamp { get; set; }

    public override string ToString()
    {
        return $"PlayerEventData(PlayerName={PlayerName}, PlayerId={PlayerId}, TimeStamp={TimeStamp})";
    }
}
