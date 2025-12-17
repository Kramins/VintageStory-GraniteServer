namespace GraniteServer.Api.Models;

public class UpdateInventorySlotRequestDTO
{
    public string Class { get; set; }
    public int Id { get; set; }
    public int SlotIndex { get; set; }
    public int? StackSize { get; set; }
}
