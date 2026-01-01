using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using GraniteServer.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace GraniteServerMod.Api.HostedServices;

public class PlayerSessionHostedService : IHostedService, IDisposable
{
    private readonly ICoreServerAPI _api;
    private readonly IServiceProvider _services;

    private CancellationTokenSource? _cts;
    private readonly ConcurrentBag<Task> _pending = new();

    public PlayerSessionHostedService(IServiceProvider services, ICoreServerAPI api)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _api.Event.PlayerJoin += OnPlayerJoin;
        _api.Event.PlayerLeave += OnPlayerLeave;
        return Task.CompletedTask;
    }

    private void OnPlayerLeave(IServerPlayer byPlayer)
    {
        RunScoped(
            async (sp, player, ct) =>
            {
                var tracker = sp.GetRequiredService<PlayerSessionTracker>();
                // If your tracker has async API prefer that; otherwise wrap sync call
                tracker.OnPlayerLeave(player);
                await Task.CompletedTask;
            },
            byPlayer
        );
    }

    private void OnPlayerJoin(IServerPlayer byPlayer)
    {
        RunScoped(
            async (sp, player, ct) =>
            {
                var tracker = sp.GetRequiredService<PlayerSessionTracker>();
                // Prefer async if available
                tracker.OnPlayerJoin(player);
                await Task.CompletedTask;
            },
            byPlayer
        );
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _api.Event.PlayerJoin -= OnPlayerJoin;
        _api.Event.PlayerLeave -= OnPlayerLeave;

        _cts?.Cancel();

        try
        {
            await Task.WhenAll(_pending.ToArray());
        }
        catch (Exception ex)
        {
            _api.Logger.Error($"[PlayerSessionHostedService] Error awaiting pending tasks: {ex}");
        }
    }

    private void RunScoped(
        System.Func<IServiceProvider, IServerPlayer, CancellationToken, Task> handler,
        IServerPlayer player
    )
    {
        var ct = _cts?.Token ?? CancellationToken.None;

        var task = Task.Run(
            async () =>
            {
                try
                {
                    using var scope = _services.CreateScope();
                    await handler(scope.ServiceProvider, player, ct);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    try
                    {
                        _api.Logger.Error($"[PlayerSessionHostedService] Handler error: {ex}");
                    }
                    catch { }
                }
            },
            ct
        );

        _pending.Add(task);
    }

    public void Dispose()
    {
        _cts?.Dispose();
    }
}
