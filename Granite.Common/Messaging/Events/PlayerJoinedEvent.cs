using GraniteServer.Messaging.Common;

namespace GraniteServer.Messaging.Events;

[ClientEvent]
public class PlayerJoinedEvent : EventMessage<PlayerJoinedEventData> { }

public class PlayerJoinedEventData : PlayerCommonEventData { }
