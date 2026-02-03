using System;

namespace Granite.Common.Dto;

public record BanRequestDTO
{
    public string? IssuedBy { get; init; }
    public string? Reason { get; init; }
    public DateTime? UntilDate { get; init; }
}
