using System;
using System.Threading;
using System.Threading.Tasks;
using Granite.Common.Messaging.Events;
using GraniteServer.Messaging.Events;
using GraniteServer.Mod;
using GraniteServer.Services;
using Microsoft.Extensions.Hosting;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace GraniteServer.HostedServices;

/// <summary>
/// Hosted service that announces when the server is ready and running.
/// Sends ServerReadyEvent to the control plane once the server reaches RunGame phase
/// and SignalR connection is established.
/// </summary>
public class ServerReadyHostedService : IHostedService, IDisposable
{
    private readonly ICoreServerAPI _api;
    private readonly ClientMessageBusService _messageBus;
    private readonly ILogger _logger;
    private readonly SignalRConnectionState _connectionState;
    private readonly GraniteModConfig _config;
    private CancellationTokenSource? _cts;
    private bool _readyEventSent;

    public ServerReadyHostedService(
        ICoreServerAPI api,
        ClientMessageBusService messageBus,
        ILogger logger,
        SignalRConnectionState connectionState,
        GraniteModConfig config
    )
    {
        _api = api;
        _messageBus = messageBus;
        _logger = logger;
        _connectionState = connectionState;
        _config = config;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.Notification("[ServerReady] Starting server ready notification service...");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Register event handler for server run phase
        _api.Event.ServerRunPhase(EnumServerRunPhase.RunGame, OnServerRunning);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.Notification("[ServerReady] Stopping server ready notification service...");
        _cts?.Cancel();
        _cts?.Dispose();
        return Task.CompletedTask;
    }


    private void OnServerRunning()
    {
        _logger.Notification("[ServerReady] Server has reached RunGame phase, waiting for SignalR connection...");
        
        // Start background task to wait for connection before announcing ready
        _ = WaitForConnectionAndAnnounceReadyAsync(_cts?.Token ?? CancellationToken.None);
    }

    private async Task WaitForConnectionAndAnnounceReadyAsync(CancellationToken token)
    {
        try
        {
            // Wait for SignalR connection
            while (!_connectionState.IsConnected && !token.IsCancellationRequested)
            {
                await Task.Delay(1000, token);
            }

            if (token.IsCancellationRequested)
                return;

            // Connection established, announce server ready
            if (!_readyEventSent)
            {
                AnnounceServerReady();
                _readyEventSent = true;
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger.Error($"[ServerReady] Error waiting for connection: {ex.Message}");
        }
    }

    private void AnnounceServerReady()
    {
        try
        {
            _logger.Notification("[ServerReady] Announcing server ready to control plane...");

            var readyEvent = _messageBus.CreateEvent<ServerReadyEvent>(
                _config.ServerId,
                e =>
                {
                    e.Data.StartedAt = DateTime.UtcNow;
                    e.Data.ServerVersion = _api.World.SeaLevel.ToString(); // Using SeaLevel as a proxy for version info
                }
            );

            _messageBus.Publish(readyEvent);
            _logger.Notification("[ServerReady] Server ready event sent successfully.");
        }
        catch (Exception ex)
        {
            _logger.Error($"[ServerReady] Failed to announce server ready: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _cts?.Dispose();
    }
}
