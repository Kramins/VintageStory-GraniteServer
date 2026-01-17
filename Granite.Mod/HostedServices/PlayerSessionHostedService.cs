
using GraniteServer.Messaging.Events;
using GraniteServer.Services;
using Microsoft.Extensions.Hosting;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace GraniteServer.HostedServices;

public class PlayerSessionHostedService : IHostedService, IDisposable
{
    private readonly ICoreServerAPI _api;
    private readonly MessageBusService _messageBus;
    private CancellationTokenSource? _cts;

    // private readonly List<Task> _pending = new();
    private readonly object _lockObject = new();
    private bool _isShuttingDown;
    private readonly ILogger _logger;

    public PlayerSessionHostedService(
        ICoreServerAPI api,
        MessageBusService messageBus,
        ILogger logger
    )
    {
        _api = api;
        _messageBus = messageBus;
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

        var playerSessionId = byPlayer.ServerData.CustomPlayerData["GraniteSessionId"];
        _messageBus.Publish(
            new PlayerLeaveEvent()
            {
                Data = new PlayerLeaveEventData
                {
                    PlayerId = byPlayer.PlayerUID,
                    PlayerName = byPlayer.PlayerName,
                    SessionId = playerSessionId.ToString(),
                    IpAddress = byPlayer.IpAddress,
                },
            }
        );
        byPlayer.ServerData.CustomPlayerData.Remove("GraniteSessionId");
    }

    private void OnPlayerJoin(IServerPlayer byPlayer)
    {
        if (_isShuttingDown)
            return;
        var playerSessionId = Guid.NewGuid();
        byPlayer.ServerData.CustomPlayerData["GraniteSessionId"] = playerSessionId.ToString();
        _messageBus.Publish(
            new PlayerJoinedEvent()
            {
                Data = new PlayerJoinedEventData
                {
                    PlayerId = byPlayer.PlayerUID,
                    PlayerName = byPlayer.PlayerName,
                    SessionId = playerSessionId.ToString(),
                    IpAddress = byPlayer.IpAddress,
                },
            }
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

    public void Dispose()
    {
        _cts?.Dispose();
        // _pending.Clear();
    }
}
