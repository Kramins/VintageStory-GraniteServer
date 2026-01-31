using Fluxor;
using Granite.Common.Dto;
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
        IEventHandler<PlayerKickedEvent>
{
    private readonly IDispatcher _dispatcher;
    private readonly ILogger<PlayerEventHandlers> _logger;

    public PlayerEventHandlers(IDispatcher dispatcher, ILogger<PlayerEventHandlers> logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    Task IEventHandler<PlayerWhitelistedEvent>.Handle(PlayerWhitelistedEvent @event)
    {
        var playerEventData = @event.Data!;
        _logger.LogInformation(
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
        _logger.LogInformation(
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
        _logger.LogInformation(
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
        _logger.LogInformation(
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
        _logger.LogInformation(
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

        return Task.CompletedTask;
    }

    Task IEventHandler<PlayerJoinedEvent>.Handle(PlayerJoinedEvent @event)
    {
        var playerEventData = @event.Data!;
        _logger.LogInformation(
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
        _logger.LogInformation(
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

        return Task.CompletedTask;
    }

    Task IEventHandler.Handle(object command)
    {
        throw new NotImplementedException();
    }
}
