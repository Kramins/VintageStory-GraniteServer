using Granite.Common.Dto;
using GraniteServer.Messaging.Common;

namespace GraniteServer.Messaging.Events;

public class ServerConfigSyncedEvent : EventMessage<ServerConfigSyncedEventData> { }

public class ServerConfigSyncedEventData
{
    public ServerConfigDTO Config { get; set; } = new();
}
