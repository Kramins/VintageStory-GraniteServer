using System;
using GraniteServer.Messaging.Common;

namespace GraniteServer.Messaging.Events;

public class PlayerInventorySlotUpdatedEvent : EventMessage<PlayerInventorySlotUpdatedEventData> { }

public class PlayerInventorySlotUpdatedEventData : PlayerCommonEventData
{
    public string InventoryName { get; set; } = string.Empty;
    public int SlotIndex { get; set; }
    public int EntityId { get; set; }
    public string? EntityClass { get; set; }
    public string? Name { get; set; }
    public int StackSize { get; set; }
}
