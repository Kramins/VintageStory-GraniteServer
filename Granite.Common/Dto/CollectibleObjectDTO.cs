namespace Granite.Common.Dto;

public class CollectibleObjectDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int MaxStackSize { get; set; }
    public string? Class { get; set; }
}
