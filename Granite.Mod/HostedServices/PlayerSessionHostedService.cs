
using GraniteServer.Messaging.Events;
using GraniteServer.Mod;
using GraniteServer.Services;
using Microsoft.Extensions.Hosting;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace GraniteServer.HostedServices;

public class PlayerSessionHostedService : IHostedService, IDisposable
{
    private readonly ICoreServerAPI _api;
    private readonly MessageBusService _messageBus;
    private readonly GraniteModConfig _config;
    private CancellationTokenSource? _cts;

    // private readonly List<Task> _pending = new();
    private readonly object _lockObject = new();
    private bool _isShuttingDown;
    private readonly ILogger _logger;

    public PlayerSessionHostedService(
        ICoreServerAPI api,
        MessageBusService messageBus,
        GraniteModConfig config,
        ILogger logger
    )
    {
        _api = api;
        _messageBus = messageBus;
        _config = config;
        _logger = logger;
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
        if (_isShuttingDown)
            return;

        var playerSessionId = Guid.Parse(byPlayer.ServerData.CustomPlayerData["GraniteSessionId"]);
        var leaveEvent = _messageBus.CreateEvent<PlayerLeaveEvent>(
            _config.ServerId,
            e =>
            {
                e.Data!.PlayerUID = byPlayer.PlayerUID;
                e.Data!.PlayerName = byPlayer.PlayerName;
                e.Data!.SessionId = playerSessionId;
                e.Data!.IpAddress = byPlayer.IpAddress;
            }
        );
        _messageBus.Publish(leaveEvent);
        byPlayer.ServerData.CustomPlayerData.Remove("GraniteSessionId");
    }

    private void OnPlayerJoin(IServerPlayer byPlayer)
    {
        if (_isShuttingDown)
            return;
        var playerSessionId = Guid.NewGuid();
        byPlayer.ServerData.CustomPlayerData["GraniteSessionId"] = playerSessionId.ToString();
        var joinEvent = _messageBus.CreateEvent<PlayerJoinedEvent>(
            _config.ServerId,
            e =>
            {
                e.Data!.PlayerUID = byPlayer.PlayerUID;
                e.Data!.PlayerName = byPlayer.PlayerName;
                e.Data!.SessionId = playerSessionId;
                e.Data!.IpAddress = byPlayer.IpAddress;
            }
        );
        _messageBus.Publish(joinEvent);
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

    public void Dispose()
    {
        _cts?.Dispose();
        // _pending.Clear();
    }
}
