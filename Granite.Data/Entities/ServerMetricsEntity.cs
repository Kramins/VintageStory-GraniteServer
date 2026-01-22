using System;

namespace GraniteServer.Data.Entities;

public class ServerMetricsEntity
{
    public Guid Id { get; set; }
    public Guid ServerId { get; set; }
    public DateTime RecordedAt { get; set; }
    
    // Navigation properties
    public ServerEntity? Server { get; set; }
    public float CpuUsagePercent { get; set; }
    public float MemoryUsageMB { get; set; }
    public int ActivePlayerCount { get; set; }
}
