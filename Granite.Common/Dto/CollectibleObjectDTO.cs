namespace Granite.Common.Dto;

public record CollectibleObjectDTO
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public int MaxStackSize { get; init; }
    public string? Class { get; init; }
}
