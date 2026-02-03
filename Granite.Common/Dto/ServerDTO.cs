using System;

namespace Granite.Common.Dto;

public record ServerDTO
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsOnline { get; init; }
    public DateTime? LastSeenAt { get; init; }
}
