using System;

namespace GraniteServer.Messaging.Common;

public class PlayerCommonEventData
{
    public string PlayerName { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public string IpAddress { get; set; }
}
