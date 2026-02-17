using GraniteServer.Messaging.Events;
using GraniteServer.Mod;
using GraniteServer.Services;
using Microsoft.Extensions.Hosting;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace GraniteServer.HostedServices;

public class PlayerSessionHostedService : GraniteHostedServiceBase
{
    private readonly ICoreServerAPI _api;
    private readonly ClientMessageBusService _messageBus;
    private readonly GraniteModConfig _config;
    private CancellationTokenSource? _cts;

    // private readonly List<Task> _pending = new();
    private bool _isShuttingDown;

    public PlayerSessionHostedService(
        ICoreServerAPI api,
        ClientMessageBusService messageBus,
        GraniteModConfig config,
        ILogger logger
    )
        : base(messageBus, logger)
    {
        _api = api;
        _messageBus = messageBus;
        _config = config;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        LogNotification("Starting player session tracking service...");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _api.Event.PlayerJoin += OnPlayerJoin;
        _api.Event.PlayerLeave += OnPlayerLeave;

        return Task.CompletedTask;
    }

    private void OnPlayerLeave(IServerPlayer byPlayer)
    {
        if (_isShuttingDown)
            return;
        PublishLeaveFromPlayer(byPlayer);
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

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _isShuttingDown = true;
        _api.Event.PlayerJoin -= OnPlayerJoin;
        _api.Event.PlayerLeave -= OnPlayerLeave;

        _cts?.Cancel();

        try
        {
            // Flush sessions for any players still connected at shutdown
            foreach (var player in _api.Server.Players)
            {
                PublishLeaveFromPlayer(player);
            }
        }
        catch (Exception ex)
        {
            LogError($"Failed to flush active player sessions on shutdown: {ex}");
        }

        try
        {
            // lock (_lockObject)
            // {
            //     if (_pending.Count > 0)
            //     {
            //         LogNotification("Waiting for {0} pending tasks", _pending.Count);
            //     }
            // }

            // await Task.WhenAll(_pending);
        }
        catch (OperationCanceledException)
        {
            LogNotification("Player session hosted service shutdown cancelled");
        }
        catch (Exception ex)
        {
            LogError($"Error awaiting pending tasks during shutdown: {ex}");
        }

        return base.StopAsync(cancellationToken);
    }

    private void PublishLeaveFromPlayer(IServerPlayer player)
    {
        if (!player.ServerData.CustomPlayerData.TryGetValue("GraniteSessionId", out var sessionObj))
        {
            return;
        }

        if (!Guid.TryParse(sessionObj, out var sessionId))
        {
            return;
        }

        var leaveEvent = _messageBus.CreateEvent<PlayerLeaveEvent>(
            _config.ServerId,
            e =>
            {
                e.Data!.PlayerUID = player.PlayerUID;
                e.Data!.PlayerName = player.PlayerName;
                e.Data!.SessionId = sessionId;
                e.Data!.IpAddress = player.IpAddress;
            }
        );

        try
        {
            _messageBus.Publish(leaveEvent);
            player.ServerData.CustomPlayerData.Remove("GraniteSessionId");
        }
        catch (Exception ex)
        {
            LogError($"Failed to publish leave event for {player.PlayerUID}: {ex}");
        }
    }

    public void Dispose()
    {
        _cts?.Dispose();
        // _pending.Clear();
    }
}
