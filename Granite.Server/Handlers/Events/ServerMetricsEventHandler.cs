using System;
using System.Threading.Tasks;
using Granite.Common.Messaging.Events;
using GraniteServer.Data;
using GraniteServer.Data.Entities;
using GraniteServer.Messaging.Handlers.Events;
using Microsoft.Extensions.Logging;

namespace Granite.Server.Handlers.Events;

public class ServerMetricsEventHandler : IEventHandler<ServerMetricsEvent>
{
    private readonly GraniteDataContext _dataContext;
    private readonly ILogger<ServerMetricsEventHandler> _logger;

    public ServerMetricsEventHandler(
        GraniteDataContext dataContext,
        ILogger<ServerMetricsEventHandler> logger
    )
    {
        _dataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    async Task IEventHandler<ServerMetricsEvent>.Handle(ServerMetricsEvent @event)
    {
        try
        {
            if (@event?.Data == null)
            {
                _logger.LogWarning("[Metrics] Received ServerMetricsEvent with null data, skipping");
                return;
            }

            var metricsEntity = new ServerMetricsEntity
            {
                Id = Guid.NewGuid(),
                ServerId = @event.OriginServerId,
                RecordedAt = DateTime.UtcNow,
                CpuUsagePercent = @event.Data.CpuUsagePercent,
                MemoryUsageMB = @event.Data.MemoryUsageMB,
                ActivePlayerCount = @event.Data.ActivePlayerCount,
            };

            _dataContext.ServerMetrics.Add(metricsEntity);
            await _dataContext.SaveChangesAsync();

            _logger.LogDebug(
                "[Metrics] Persisted metrics for server {ServerId}: CPU={CpuPercent:F1}% MEM={MemMB:F1}MB Players={PlayerCount}",
                @event.OriginServerId,
                @event.Data.CpuUsagePercent,
                @event.Data.MemoryUsageMB,
                @event.Data.ActivePlayerCount
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[Metrics] Error persisting server metrics for server {ServerId}",
                @event?.OriginServerId
            );
        }
    }
}
