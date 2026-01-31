using System.Collections.Generic;

namespace Granite.Common.Dto;

public record InventoryDTO
{
    public string Name { get; init; } = string.Empty;
    public List<InventorySlotDTO> Slots { get; init; } = new List<InventorySlotDTO>();
}

public record InventorySlotDTO
{
    public string? EntityClass { get; init; }
    public int EntityId { get; init; }
    public string? Name { get; init; }
    public int SlotIndex { get; init; }
    public int StackSize { get; init; }
}

public record PlayerDetailsDTO : PlayerDTO
{
    public Dictionary<string, InventoryDTO> Inventories { get; init; } =
        new Dictionary<string, InventoryDTO>();
}
