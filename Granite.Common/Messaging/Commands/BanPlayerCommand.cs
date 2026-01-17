using System;

namespace GraniteServer.Messaging.Commands;

public class BanPlayerCommand : CommandMessage<BanPlayerCommandData> { }

public class BanPlayerCommandData
{
    public string PlayerId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime? ExpirationDate { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public string IssuedBy { get; set; } = string.Empty;
}
