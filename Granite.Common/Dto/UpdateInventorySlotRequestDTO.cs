namespace Granite.Common.Dto;

public class UpdateInventorySlotRequestDTO
{
    public string EntityClass { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public int SlotIndex { get; set; }
    public int? StackSize { get; set; }
}
