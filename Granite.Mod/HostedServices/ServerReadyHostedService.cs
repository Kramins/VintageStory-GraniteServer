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
public class ServerReadyHostedService : GraniteHostedServiceBase
{
    private readonly ICoreServerAPI _api;
    private readonly ClientMessageBusService _messageBus;
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
        : base(messageBus, logger)
    {
        _api = api;
        _messageBus = messageBus;
        _connectionState = connectionState;
        _config = config;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        LogNotification("Starting server ready notification service...");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Register event handler for server run phase
        _api.Event.ServerRunPhase(EnumServerRunPhase.RunGame, OnServerRunning);

        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        return base.StopAsync(cancellationToken);
    }

    private void OnServerRunning()
    {
        LogNotification("Server has reached RunGame phase, waiting for SignalR connection...");

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
            LogError($"Error waiting for connection: {ex.Message}");
        }
    }

    private void AnnounceServerReady()
    {
        try
        {
            LogNotification("Announcing server ready to control plane...");

            var readyEvent = _messageBus.CreateEvent<ServerReadyEvent>(
                _config.ServerId,
                e =>
                {
                    e.Data.StartedAt = DateTime.UtcNow;
                    e.Data.ServerVersion = _api.World.SeaLevel.ToString(); // Using SeaLevel as a proxy for version info
                }
            );

            _messageBus.Publish(readyEvent);
            LogNotification("Server ready event sent successfully.");
        }
        catch (Exception ex)
        {
            LogError($"Failed to announce server ready: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _cts?.Dispose();
    }
}
