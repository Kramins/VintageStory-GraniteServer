namespace GraniteServer.Api.Models;

public class UpdateInventorySlotRequestDTO
{
    public int? Id { get; set; }
    public int SlotIndex { get; set; }
    public int? StackSize { get; set; }
}
