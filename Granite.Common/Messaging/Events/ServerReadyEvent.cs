using GraniteServer.Messaging.Common;

namespace GraniteServer.Messaging.Events;

[ClientEvent]
public class ServerReadyEvent : EventMessage<ServerReadyEventData> { }

public class ServerReadyEventData
{
    public DateTime StartedAt { get; set; }
    public string ServerVersion { get; set; } = string.Empty;
}
