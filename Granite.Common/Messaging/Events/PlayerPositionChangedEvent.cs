using System;
using GraniteServer.Messaging;
using GraniteServer.Messaging.Events;

namespace Granite.Common.Messaging.Events;

[ClientEvent]
public class PlayerPositionChangedEvent : EventMessage<PlayerPositionChangedEventData> { }

public class PlayerPositionChangedEventData
{
    public string PlayerUID { get; set; } = string.Empty;
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
}
