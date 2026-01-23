using System;
using System.Threading;
using System.Threading.Tasks;
using Granite.Common.Dto;
using GraniteServer.Messaging.Events;
using GraniteServer.Mod;
using GraniteServer.Services;
using Microsoft.Extensions.Hosting;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace GraniteServer.HostedServices;

/// <summary>
/// Hosted service that syncs server configuration to the control plane on startup.
/// Uses a periodic check to sync once SignalR connection is established.
/// </summary>
public class ServerConfigSyncHostedService : IHostedService, IDisposable
{
    private readonly ICoreServerAPI _api;
    private readonly ClientMessageBusService _messageBus;
    private readonly ILogger _logger;
    private readonly SignalRConnectionState _connectionState;
    private readonly GraniteModConfig _config;
    private CancellationTokenSource? _cts;
    private bool _initialSyncCompleted;

    public ServerConfigSyncHostedService(
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
        _logger.Notification("[ServerConfig] Starting server config sync service...");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Start background task to wait for connection and sync
        _ = WaitForConnectionAndSyncAsync(_cts.Token);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.Notification("[ServerConfig] Stopping server config sync service...");
        _cts?.Cancel();
        _cts?.Dispose();
        return Task.CompletedTask;
    }

    private async Task WaitForConnectionAndSyncAsync(CancellationToken token)
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

            // Connection established, perform initial sync
            if (!_initialSyncCompleted)
            {
                SyncServerConfig();
                _initialSyncCompleted = true;
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger.Error($"[ServerConfig] Error waiting for connection: {ex.Message}");
        }
    }

    private void SyncServerConfig()
    {
        try
        {
            _logger.Notification("[ServerConfig] Syncing server configuration to control plane...");

            var config = ReadServerConfig();
            var syncEvent = _messageBus.CreateEvent<ServerConfigSyncedEvent>(
                _config.ServerId,
                e =>
                {
                    e.Data.Config = config;
                }
            );

            _messageBus.Publish(syncEvent);
            _logger.Notification("[ServerConfig] Server configuration synced successfully.");
        }
        catch (Exception ex)
        {
            _logger.Error($"[ServerConfig] Failed to sync server configuration: {ex.Message}");
        }
    }

    private ServerConfigDTO ReadServerConfig()
    {
        var serverConfig = _api.Server.Config;
        return new ServerConfigDTO
        {
            Port = serverConfig.Port,
            ServerName = serverConfig.ServerName,
            WelcomeMessage = serverConfig.WelcomeMessage,
            MaxClients = serverConfig.MaxClients,
            Password = serverConfig.Password ?? string.Empty,
            MaxChunkRadius = serverConfig.MaxChunkRadius,
            WhitelistMode = serverConfig.WhitelistMode.ToString(),
            AllowPvP = serverConfig.AllowPvP,
            AllowFireSpread = serverConfig.AllowFireSpread,
            AllowFallingBlocks = serverConfig.AllowFallingBlocks,
        };
    }

    public void Dispose()
    {
        _cts?.Dispose();
    }
}
