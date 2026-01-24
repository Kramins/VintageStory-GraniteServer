using System;

namespace GraniteServer.Data.Entities;

public class PlayerInventorySlotEntity
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public Guid ServerId { get; set; }
    public string InventoryName { get; set; } = string.Empty;
    public int SlotIndex { get; set; }
    public int EntityId { get; set; }
    public string? EntityClass { get; set; }
    public string? Name { get; set; }
    public int StackSize { get; set; }
    public DateTime LastUpdated { get; set; }

    // Navigation properties
    public PlayerEntity? Player { get; set; }
}
