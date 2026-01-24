using GraniteServer.Messaging.Common;

namespace GraniteServer.Messaging.Events;

[ClientEvent]
public class PlayerWhitelistedEvent : EventMessage<PlayerWhitelistedEventData> { }

public class PlayerWhitelistedEventData : PlayerCommonEventData { }
