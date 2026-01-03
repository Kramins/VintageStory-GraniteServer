using System;
using System.Threading;
using System.Threading.Tasks;
using GraniteServer.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vintagestory.API.Server;

namespace GraniteServer.Api.HostedServices;

public class ModSystemHostedService : IHostedService, IDisposable
{
    private IServiceProvider _serviceProvider;
    private ICoreServerAPI _api;

    private CancellationTokenSource _cts = new CancellationTokenSource();

    public ModSystemHostedService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _api = serviceProvider.GetService<ICoreServerAPI>()!;
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _api.Event.ServerRunPhase(EnumServerRunPhase.GameReady, OnGameReady);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the game is ready.
    /// Scope must be created here as this method is called from another thread.
    /// </summary>
    private void OnGameReady()
    {
        using var scope = _serviceProvider.CreateScope();
        var modManagementService = scope.ServiceProvider.GetRequiredService<ModManagementService>();

        modManagementService.SyncModsAsync(_cts.Token).Wait();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cts.Cancel();
        return Task.CompletedTask;
    }
}
