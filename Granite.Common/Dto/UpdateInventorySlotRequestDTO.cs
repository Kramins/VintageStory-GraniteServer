namespace Granite.Common.Dto;

public record UpdateInventorySlotRequestDTO
{
    public string EntityClass { get; init; } = string.Empty;
    public int EntityId { get; init; }
    public int SlotIndex { get; init; }
    public int? StackSize { get; init; }
}
