using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Granite.Common.Messaging.Events;
using GraniteServer.Services;
using Microsoft.Extensions.Hosting;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace GraniteServer.HostedServices;

public class ServerMetricsHostedService : IHostedService, IDisposable
{
    private readonly ICoreServerAPI _api;
    private readonly MessageBusService _messageBus;
    private readonly ILogger _logger;
    private readonly SignalRConnectionState _connectionState;

    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;

    private readonly Process _process;
    private TimeSpan _prevCpu;
    private DateTime _prevTimeUtc;
    private readonly int _processorCount;

    private const int IntervalSeconds = 30;

    public ServerMetricsHostedService(
        ICoreServerAPI api,
        MessageBusService messageBus,
        ILogger logger,
        SignalRConnectionState connectionState
    )
    {
        _api = api;
        _messageBus = messageBus;
        _logger = logger;
        _connectionState = connectionState;
        _process = Process.GetCurrentProcess();
        _prevCpu = _process.TotalProcessorTime;
        _prevTimeUtc = DateTime.UtcNow;
        _processorCount = Environment.ProcessorCount;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.Notification("[Metrics] Starting server metrics publisher...");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(IntervalSeconds));

        _ = RunAsync(_cts.Token);
        return Task.CompletedTask;
    }

    private async Task RunAsync(CancellationToken token)
    {
        try
        {
            while (_timer != null && await _timer.WaitForNextTickAsync(token))
            {
                if (!_connectionState.IsConnected)
                {
                    _logger.Debug("[Metrics] SignalR not connected; skipping publish.");
                    continue;
                }

                PublishMetrics();
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger.Error($"[Metrics] Error in metrics loop: {ex.Message}");
        }
    }

    private void PublishMetrics()
    {
        try
        {
            var nowCpu = _process.TotalProcessorTime;
            var nowUtc = DateTime.UtcNow;

            var cpuDeltaMs = (nowCpu - _prevCpu).TotalMilliseconds;
            var elapsedMs = (nowUtc - _prevTimeUtc).TotalMilliseconds;
            float cpuPercent = 0f;
            if (elapsedMs > 0 && _processorCount > 0)
            {
                cpuPercent = (float)(cpuDeltaMs / (elapsedMs * _processorCount) * 100.0);
                if (cpuPercent < 0) cpuPercent = 0f;
            }

            _prevCpu = nowCpu;
            _prevTimeUtc = nowUtc;

            var memBytes = GC.GetTotalMemory(false);
            var memMb = (float)(memBytes / (1024.0 * 1024.0));

            var activePlayers = _api.World?.AllOnlinePlayers?.Length ?? 0;

            _messageBus.Publish(
                new ServerMetricsEvent
                {
                    Data = new ServerMetricsEventData
                    {
                        CpuUsagePercent = cpuPercent,
                        MemoryUsageMB = memMb,
                        ActivePlayerCount = activePlayers,
                    },
                }
            );

            _logger.Debug(
                $"[Metrics] Published metrics: CPU={cpuPercent:F1}% MEM={memMb:F1}MB Players={activePlayers}"
            );
        }
        catch (Exception ex)
        {
            _logger.Error($"[Metrics] Failed to publish metrics: {ex.Message}");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.Notification("[Metrics] Stopping server metrics publisher...");
        _cts?.Cancel();
        _timer?.Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _cts?.Dispose();
        _timer?.Dispose();
    }
}
