using System;
using System.Collections.Generic;
using GraniteServer.Messaging.Common;

namespace GraniteServer.Messaging.Events;

[ClientEvent]
public class PlayerInventorySnapshotEvent : EventMessage<PlayerInventorySnapshotEventData> { }

public class PlayerInventorySnapshotEventData : PlayerCommonEventData
{
    public Dictionary<string, List<InventorySlotEventData>> Inventories { get; set; } =
        new Dictionary<string, List<InventorySlotEventData>>();
}

public class InventorySlotEventData
{
    public int SlotIndex { get; set; }
    public int EntityId { get; set; }
    public string? EntityClass { get; set; }
    public string? Name { get; set; } // TODO: Review, I don't think Name is needed here
    public int StackSize { get; set; }
}
