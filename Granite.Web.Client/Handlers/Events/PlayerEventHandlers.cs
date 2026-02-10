using Fluxor;
using Granite.Common.Dto;
using Granite.Common.Messaging.Events;
using Granite.Web.Client.Store.Features.Map;
using Granite.Web.Client.Store.Features.Players;
using GraniteServer.Messaging.Events;
using GraniteServer.Messaging.Handlers.Events;

namespace Granite.Web.Client.Handlers.Events;

/// <summary>
/// Client-side event handler for player-related events received from the server via SignalR.
/// Dispatches Fluxor actions to update the client state in response to server events.
/// </summary>
public class PlayerEventHandlers
    : IEventHandler<PlayerWhitelistedEvent>,
        IEventHandler<PlayerUnwhitelistedEvent>,
        IEventHandler<PlayerBannedEvent>,
        IEventHandler<PlayerUnbannedEvent>,
        IEventHandler<PlayerLeaveEvent>,
        IEventHandler<PlayerJoinedEvent>,
        IEventHandler<PlayerKickedEvent>,
        IEventHandler<PlayerPositionChangedEvent>
{
    private readonly IDispatcher _dispatcher;
    private readonly ILogger<PlayerEventHandlers> _logger;
    private readonly IState<PlayersState> _playersState;

    public PlayerEventHandlers(
        IDispatcher dispatcher,
        ILogger<PlayerEventHandlers> logger,
        IState<PlayersState> playersState
    )
    {
        _dispatcher = dispatcher;
        _logger = logger;
        _playersState = playersState;
    }

    Task IEventHandler<PlayerPositionChangedEvent>.Handle(PlayerPositionChangedEvent command)
    {
        var data = command.Data!;

        // Look up player name from players state
        var player = _playersState.Value.Players.FirstOrDefault(p => p.PlayerUID == data.PlayerUID);
        
        // Use actual player name if available, otherwise use truncated UID without "Player" prefix
        var playerName = player?.Name ?? data.PlayerUID.Substring(0, 8);
        
        // Log warning if player not found in state (for debugging)
        if (player == null)
        {
            _logger.LogWarning(
                "Player with UID {PlayerUID} not found in PlayersState. Total players in state: {Count}",
                data.PlayerUID,
                _playersState.Value.Players.Count
            );
        }

        _logger.LogDebug(
            "Player {PlayerName} ({PlayerUID}) moved to position ({X}, {Y}, {Z})",
            playerName,
            data.PlayerUID,
            data.X,
            data.Y,
            data.Z
        );

        // Dispatch action to update map state
        _dispatcher.Dispatch(
            new UpdatePlayerMapPositionAction(data.PlayerUID, data.X, data.Z, playerName)
        );

        return Task.CompletedTask;
    }

    Task IEventHandler<PlayerWhitelistedEvent>.Handle(PlayerWhitelistedEvent @event)
    {
        var playerEventData = @event.Data!;
        _logger.LogDebug(
            "Player {PlayerName} ({PlayerUID}) was whitelisted on server {ServerId}",
            playerEventData.PlayerName,
            playerEventData.PlayerUID,
            @event.OriginServerId
        );

        _dispatcher.Dispatch(
            new UpdatePlayerWhitelistStatusAction(
                playerEventData.PlayerUID,
                @event.OriginServerId,
                true,
                playerEventData.PlayerName
            )
        );

        return Task.CompletedTask;
    }

    Task IEventHandler<PlayerUnwhitelistedEvent>.Handle(PlayerUnwhitelistedEvent @event)
    {
        var playerEventData = @event.Data!;
        _logger.LogDebug(
            "Player {PlayerName} ({PlayerUID}) was removed from whitelist on server {ServerId}",
            playerEventData.PlayerName,
            playerEventData.PlayerUID,
            @event.OriginServerId
        );

        _dispatcher.Dispatch(
            new UpdatePlayerWhitelistStatusAction(
                playerEventData.PlayerUID,
                @event.OriginServerId,
                false,
                playerEventData.PlayerName
            )
        );

        return Task.CompletedTask;
    }

    Task IEventHandler<PlayerBannedEvent>.Handle(PlayerBannedEvent @event)
    {
        var playerEventData = @event.Data!;
        _logger.LogDebug(
            "Player {PlayerName} ({PlayerUID}) was banned on server {ServerId}. Reason: {Reason}",
            playerEventData.PlayerName,
            playerEventData.PlayerUID,
            @event.OriginServerId,
            playerEventData.Reason
        );

        _dispatcher.Dispatch(
            new UpdatePlayerBanStatusAction(
                playerEventData.PlayerUID,
                @event.OriginServerId,
                true,
                playerEventData.Reason,
                playerEventData.PlayerName
            )
        );

        return Task.CompletedTask;
    }

    Task IEventHandler<PlayerUnbannedEvent>.Handle(PlayerUnbannedEvent @event)
    {
        var playerEventData = @event.Data!;
        _logger.LogDebug(
            "Player {PlayerName} ({PlayerUID}) was unbanned on server {ServerId}",
            playerEventData.PlayerName,
            playerEventData.PlayerUID,
            @event.OriginServerId
        );

        _dispatcher.Dispatch(
            new UpdatePlayerBanStatusAction(
                playerEventData.PlayerUID,
                @event.OriginServerId,
                false,
                null,
                playerEventData.PlayerName
            )
        );

        return Task.CompletedTask;
    }

    Task IEventHandler<PlayerLeaveEvent>.Handle(PlayerLeaveEvent @event)
    {
        var playerEventData = @event.Data!;
        _logger.LogDebug(
            "Player {PlayerName} ({PlayerUID}) left server {ServerId}",
            playerEventData.PlayerName,
            playerEventData.PlayerUID,
            @event.OriginServerId
        );

        _dispatcher.Dispatch(
            new UpdatePlayerConnectionStateAction(
                playerEventData.PlayerUID,
                @event.OriginServerId,
                "Disconnected",
                playerEventData.PlayerName,
                playerEventData.IpAddress ?? string.Empty
            )
        );

        // Remove player from map when they leave
        _dispatcher.Dispatch(new RemovePlayerFromMapAction(playerEventData.PlayerUID));

        return Task.CompletedTask;
    }

    Task IEventHandler<PlayerJoinedEvent>.Handle(PlayerJoinedEvent @event)
    {
        var playerEventData = @event.Data!;
        _logger.LogDebug(
            "Player {PlayerName} ({PlayerUID}) joined server {ServerId}",
            playerEventData.PlayerName,
            playerEventData.PlayerUID,
            @event.OriginServerId
        );

        _dispatcher.Dispatch(
            new UpdatePlayerConnectionStateAction(
                playerEventData.PlayerUID,
                @event.OriginServerId,
                "Connected",
                playerEventData.PlayerName,
                playerEventData.IpAddress ?? string.Empty
            )
        );

        return Task.CompletedTask;
    }

    Task IEventHandler<PlayerKickedEvent>.Handle(PlayerKickedEvent @event)
    {
        var playerEventData = @event.Data!;
        _logger.LogDebug(
            "Player {PlayerName} ({PlayerUID}) was kicked from server {ServerId}. Reason: {Reason}",
            playerEventData.PlayerName,
            playerEventData.PlayerUID,
            @event.OriginServerId,
            playerEventData.Reason
        );

        _dispatcher.Dispatch(
            new UpdatePlayerConnectionStateAction(
                playerEventData.PlayerUID,
                @event.OriginServerId,
                "Disconnected",
                playerEventData.PlayerName,
                playerEventData.IpAddress ?? string.Empty
            )
        );

        // Remove player from map when they're kicked
        _dispatcher.Dispatch(new RemovePlayerFromMapAction(playerEventData.PlayerUID));

        return Task.CompletedTask;
    }

    Task IEventHandler.Handle(object command)
    {
        throw new NotImplementedException();
    }
}
