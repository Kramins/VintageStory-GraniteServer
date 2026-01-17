using System;

namespace GraniteServer.Api.Messaging.Contracts;

public class KickPlayerCommandData
{
    public string PlayerId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public bool WaitForDisconnect { get; set; }
}
