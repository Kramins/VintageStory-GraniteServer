using GraniteServer.Messaging.Common;

namespace GraniteServer.Messaging.Events;

[ClientEvent]
public class PlayerKickedEvent : EventMessage<PlayerKickedEventData> { }

public class PlayerKickedEventData : PlayerCommonEventData
{
    public string Reason { get; set; } = string.Empty;
    public string IssuedBy { get; set; } = string.Empty;
}
