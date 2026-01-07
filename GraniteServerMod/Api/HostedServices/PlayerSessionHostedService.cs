using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraniteServer.Api.Extensions;
using GraniteServer.Api.Messaging.Events;
using GraniteServer.Api.Messaging.Contracts;
using GraniteServer.Api.Messaging.Commands;
using GraniteServer.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace GraniteServer.Api.HostedServices;

public class PlayerSessionHostedService : IHostedService, IDisposable
{
    private readonly ICoreServerAPI _api;
    private readonly IServiceProvider _servicesProvider;

    private CancellationTokenSource? _cts;

    // private readonly List<Task> _pending = new();
    private readonly object _lockObject = new();
    private bool _isShuttingDown;
    private readonly ILogger _logger;

    public PlayerSessionHostedService(
        IServiceProvider servicesProvider,
        ICoreServerAPI api,
        ILogger logger
    )
    {
        _servicesProvider = servicesProvider;
        _api = api;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var scope = _servicesProvider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<MessageBusService>();

        messageBus.Subscribe<KickPlayerCommand>(PlayerKickCommandHandler);

        _api.Event.PlayerJoin += OnPlayerJoin;
        _api.Event.PlayerLeave += OnPlayerLeave;

        return Task.CompletedTask;
    }

    private void PlayerKickCommandHandler(KickPlayerCommand command)
    {
        throw new NotImplementedException();
    }

    private void OnPlayerLeave(IServerPlayer byPlayer)
    {
        if (_isShuttingDown)
            return;

        RunScoped(
            (sp, player) =>
            {
                var tracker = sp.GetRequiredService<PlayerSessionTracker>();
                tracker.OnPlayerLeave(player);
            },
            byPlayer
        );
    }

    private void OnPlayerJoin(IServerPlayer byPlayer)
    {
        if (_isShuttingDown)
            return;

        RunScoped(
            (sp, player) =>
            {
                var tracker = sp.GetRequiredService<PlayerSessionTracker>();
                tracker.OnPlayerJoin(player);
            },
            byPlayer
        );
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _isShuttingDown = true;
        _api.Event.PlayerJoin -= OnPlayerJoin;
        _api.Event.PlayerLeave -= OnPlayerLeave;

        _cts?.Cancel();

        try
        {
            // lock (_lockObject)
            // {
            //     if (_pending.Count > 0)
            //     {
            //         _logger.Notification("Waiting for {0} pending tasks", _pending.Count);
            //     }
            // }

            // await Task.WhenAll(_pending);
        }
        catch (OperationCanceledException)
        {
            _logger.Notification("Player session hosted service shutdown cancelled");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error awaiting pending tasks during shutdown: {ex}");
        }
    }

    private void RunScoped(Action<IServiceProvider, IServerPlayer> handler, IServerPlayer player)
    {
        var ct = _cts?.Token ?? CancellationToken.None;

        Task.Run(async () =>
        {
            try
            {
                using var scope = _servicesProvider.CreateScope();
                handler(scope.ServiceProvider, player);
            }
            catch (OperationCanceledException)
            {
                _api.Logger.Debug("Player session handler was cancelled");
            }
            catch (Exception ex)
            {
                _api.Logger.Error($"Error in player session handler: {ex}");
            }
        });
    }

    public void Dispose()
    {
        _cts?.Dispose();
        // _pending.Clear();
    }
}
