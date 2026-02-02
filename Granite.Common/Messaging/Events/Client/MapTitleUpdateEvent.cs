using System;
using GraniteServer.Messaging;
using GraniteServer.Messaging.Events;

namespace Granite.Common.Messaging.Events.Client;

/// <summary>
/// Event sent from Granite.Server to Granite.Client when the title of a map chunk has been updated.
/// The client can use this to refresh map titles in its UI.
/// </summary>
[ClientEvent]
public class MapTitleUpdateEvent : EventMessage<MapTitleUpdateEventData> { }

public class MapTitleUpdateEventData
{
    public int ChunkX { get; set; }
    public int ChunkZ { get; set; }
}
