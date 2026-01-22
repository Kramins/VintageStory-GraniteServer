using GraniteServer.Messaging.Common;

namespace GraniteServer.Messaging.Events;

public class PlayerUnwhitelistedEvent : EventMessage<PlayerUnwhitelistedEventData> { }

public class PlayerUnwhitelistedEventData : PlayerCommonEventData { }
