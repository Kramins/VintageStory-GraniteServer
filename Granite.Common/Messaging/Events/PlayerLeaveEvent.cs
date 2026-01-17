using GraniteServer.Messaging.Common;

namespace GraniteServer.Messaging.Events;

public class PlayerLeaveEvent : EventMessage<PlayerLeaveEventData> { }

public class PlayerLeaveEventData : PlayerCommonEventData { }
