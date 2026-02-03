namespace Granite.Common.Dto;

public record InstallModRequest
{
    public string ModId { get; init; } = string.Empty;
}
