namespace GraniteServer.Api.Models;

public class UpdateInventorySlotRequestDTO
{
    public string EntityClass { get; set; }
    public int EntityId { get; set; }
    public int SlotIndex { get; set; }
    public int? StackSize { get; set; }
}
