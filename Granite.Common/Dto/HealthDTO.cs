using System;

namespace Granite.Common.Dto;

public record HealthDTO
{
    public string Status { get; init; } = "ok";
    public DateTime UtcNow { get; init; } = DateTime.UtcNow;
}
