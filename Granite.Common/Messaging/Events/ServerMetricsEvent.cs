using System;
using GraniteServer.Messaging;
using GraniteServer.Messaging.Events;

namespace Granite.Common.Messaging.Events;

[ClientEvent]
public class ServerMetricsEvent : EventMessage<ServerMetricsEventData> { }

public class ServerMetricsEventData
{
    public float CpuUsagePercent { get; set; }
    public float MemoryUsageMB { get; set; }
    public int ActivePlayerCount { get; set; }
    public int UpTimeSeconds { get; set; }
}
