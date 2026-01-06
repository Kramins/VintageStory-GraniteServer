using GraniteServer.Api.Messaging.Contracts;

namespace GraniteServer.Api.Messaging.Events;

public class PlayerJoinEvent : MessageBusMessage<PlayerEventData> { }
