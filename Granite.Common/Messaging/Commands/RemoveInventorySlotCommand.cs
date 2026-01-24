using System;

namespace GraniteServer.Messaging.Commands;

public class RemoveInventorySlotCommand : CommandMessage<RemoveInventorySlotCommandData> { }

public class RemoveInventorySlotCommandData
{
    public string PlayerId { get; set; } = string.Empty;
    public string InventoryName { get; set; } = string.Empty;
    public int SlotIndex { get; set; }
}
