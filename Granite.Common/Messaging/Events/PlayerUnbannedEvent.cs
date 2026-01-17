using System;
using GraniteServer.Messaging.Common;

namespace GraniteServer.Messaging.Events;

public class PlayerUnbannedEvent : EventMessage<PlayerUnbannedEventData> { }

public class PlayerUnbannedEventData : PlayerCommonEventData
{
    public string Reason { get; set; } = string.Empty;
    public string IssuedBy { get; set; } = string.Empty;
    public DateTime? UntilDate { get; set; }
}
