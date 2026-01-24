using System;

namespace GraniteServer.Messaging.Commands;

public class UpdateInventorySlotCommand : CommandMessage<UpdateInventorySlotCommandData> { }

public class UpdateInventorySlotCommandData
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public string InventoryName { get; set; } = string.Empty;
    public int SlotIndex { get; set; }
    public int ItemId { get; set; }
    public int Quantity { get; set; }
}
