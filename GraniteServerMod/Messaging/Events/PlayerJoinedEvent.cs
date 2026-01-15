using GraniteServer.Messaging.Common;

namespace GraniteServer.Messaging.Events;

public class PlayerJoinedEvent : EventMessage<PlayerJoinedEventData> { }

public class PlayerJoinedEventData : PlayerCommonEventData { }
