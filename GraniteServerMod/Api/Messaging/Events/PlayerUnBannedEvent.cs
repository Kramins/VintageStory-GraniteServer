using System;
using GraniteServer.Api.Messaging.Contracts;

namespace GraniteServer.Api.Messaging.Events;

public class PlayerUnBannedEvent : MessageBusMessage<PlayerBanEventData> { }
