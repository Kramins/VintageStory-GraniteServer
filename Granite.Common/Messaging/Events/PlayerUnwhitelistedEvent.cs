using GraniteServer.Messaging.Common;

namespace GraniteServer.Messaging.Events;

[ClientEvent]
public class PlayerUnwhitelistedEvent : EventMessage<PlayerUnwhitelistedEventData> { }

public class PlayerUnwhitelistedEventData : PlayerCommonEventData { }
