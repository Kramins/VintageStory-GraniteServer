using GraniteServer.Messaging.Common;

namespace GraniteServer.Messaging.Events;

public class PlayerWhitelistedEvent : EventMessage<PlayerWhitelistedEventData> { }

public class PlayerWhitelistedEventData : PlayerCommonEventData { }
