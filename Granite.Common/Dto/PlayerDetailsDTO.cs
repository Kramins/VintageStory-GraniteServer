using System.Collections.Generic;

namespace Granite.Common.Dto;

public class InventoryDTO
{
    public string Name { get; set; } = string.Empty;
    public List<InventorySlotDTO> Slots { get; set; } = new List<InventorySlotDTO>();
}

public class InventorySlotDTO
{
    public string? EntityClass { get; set; }
    public int EntityId { get; set; }
    public string? Name { get; set; }
    public int SlotIndex { get; set; }
    public int StackSize { get; set; }
}

public class PlayerDetailsDTO : PlayerDTO
{
    public Dictionary<string, InventoryDTO> Inventories { get; set; } =
        new Dictionary<string, InventoryDTO>();
}
