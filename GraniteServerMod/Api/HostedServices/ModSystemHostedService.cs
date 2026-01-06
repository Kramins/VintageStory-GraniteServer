using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraniteServer.Api.Extensions;
using GraniteServer.Api.Messaging.Commands;
using GraniteServer.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace GraniteServer.Api.HostedServices;

public class ModSystemHostedService : IHostedService, IDisposable
{
    private IServiceProvider _serviceProvider;
    private ICoreServerAPI _api;
    private MessageBusService _messageBus;
    private CancellationTokenSource _cts = new CancellationTokenSource();
    private IDisposable _modInstallSubscription;

    public ModSystemHostedService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        _api = serviceProvider.GetRequiredService<ICoreServerAPI>();
        _messageBus = serviceProvider.GetRequiredService<MessageBusService>();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _api.Event.ServerRunPhase(EnumServerRunPhase.GameReady, OnGameReady);

        _modInstallSubscription = _messageBus.Subscribe<InstallModCommand>(ModInstallEventHandler);

        return Task.CompletedTask;
    }

    private void ModInstallEventHandler(InstallModCommand eventData)
    {
        using var scope = _serviceProvider.CreateScope();
        var modManagementService = scope.ServiceProvider.GetRequiredService<ModManagementService>();
        modManagementService.InstallOrUpdateModAsync(eventData.Data!.ModId).Wait();
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
        _modInstallSubscription.Dispose();
        return Task.CompletedTask;
    }
}
