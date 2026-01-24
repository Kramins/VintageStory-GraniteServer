using System;
using GraniteServer.Messaging.Common;

namespace GraniteServer.Messaging.Events;

public class PlayerInventorySlotRemovedEvent : EventMessage<PlayerInventorySlotRemovedEventData> { }

public class PlayerInventorySlotRemovedEventData : PlayerCommonEventData
{
    public string InventoryName { get; set; } = string.Empty;
    public int SlotIndex { get; set; }
}
