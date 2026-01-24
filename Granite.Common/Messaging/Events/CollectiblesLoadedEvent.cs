using System;
using System.Collections.Generic;

namespace GraniteServer.Messaging.Events;

[ClientEvent(false)]
public class CollectiblesLoadedEvent : EventMessage<CollectiblesLoadedEventData> { }

public class CollectiblesLoadedEventData
{
    public List<CollectibleEventData> Collectibles { get; set; } = new List<CollectibleEventData>();
}

public class CollectibleEventData
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int MaxStackSize { get; set; }
    public string? Class { get; set; }
}
