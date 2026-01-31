namespace Granite.Common.Dto;

public record PlayerNameIdDTO
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}
