using GraniteServer.Messaging.Common;

namespace GraniteServer.Messaging.Events;

[ClientEvent]
public class PlayerLeaveEvent : EventMessage<PlayerLeaveEventData> { }

public class PlayerLeaveEventData : PlayerCommonEventData { }
