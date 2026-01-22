using System;
using GraniteServer.Messaging.Common;

namespace GraniteServer.Messaging.Events;

public class PlayerBannedEvent : EventMessage<PlayerBannedEventData> { }

public class PlayerBannedEventData : PlayerCommonEventData
{
    public string Reason { get; set; } = string.Empty;
    public string IssuedBy { get; set; } = string.Empty;
    public DateTime? UntilDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
}
