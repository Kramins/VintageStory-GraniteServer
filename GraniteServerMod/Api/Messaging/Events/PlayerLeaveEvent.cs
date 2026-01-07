using GraniteServer.Api.Messaging.Contracts;

namespace GraniteServer.Api.Messaging.Events;

public class PlayerLeaveEvent : MessageBusMessage<PlayerEventData> { }
