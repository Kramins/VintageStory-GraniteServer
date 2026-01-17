using System;

namespace Granite.Common.Dto;

public class HealthDTO
{
    public string Status { get; set; } = "ok";
    public DateTime UtcNow { get; set; } = DateTime.UtcNow;
}
