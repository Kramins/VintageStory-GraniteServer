using System;
using GraniteServer.Api.Messaging.Contracts;

namespace GraniteServer.Api.Messaging.Events;

public class PlayerUnWhitelistedEvent : MessageBusMessage<PlayerEventData> { }
