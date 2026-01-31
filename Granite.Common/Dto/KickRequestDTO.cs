namespace Granite.Common.Dto;

public record KickRequestDTO
{
    public string Reason { get; init; } = string.Empty;
}
