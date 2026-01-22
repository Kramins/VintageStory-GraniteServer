using System;
using System.Collections.Concurrent;
using Granite.Server.Services;
using GraniteServer.Messaging;
using GraniteServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Granite.Server.Hubs;

/// <summary>
/// SignalR Hub for ClientApp communication
/// </summary>
[Authorize]
public class ClientHub : Hub
{
    private readonly PersistentMessageBusService _messageBus;
    private readonly ILogger<ClientHub> _logger;
    private static readonly ConcurrentDictionary<string, IDisposable> _subscriptions = new();

    public ClientHub(PersistentMessageBusService messageBus, ILogger<ClientHub> logger)
    {
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Called when a client connects. Subscribes the client to all message bus events.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        var username = Context.User?.Identity?.Name ?? "unknown";

        _logger.LogInformation(
            "[ClientHub] User {Username} connected with connection ID {ConnectionId}",
            username,
            connectionId
        );

        // Subscribe to all messages from the message bus
        // ClientApp should receive events from all servers for management purposes
        var subscription = _messageBus
            .GetObservable()
            .Subscribe(
                message =>
                {
                    try
                    {
                        // Broadcast event to this specific client
                        Clients
                            .Client(connectionId)
                            .SendAsync("ServerEvent", new
                            {
                                id = Guid.NewGuid().ToString(),
                                eventType = message.GetType().Name,
                                data = message.Data,
                                timestamp = DateTime.UtcNow.ToString("O"),
                                source = message.OriginServerId.ToString()
                            });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "[ClientHub] Error sending event to client {ConnectionId}",
                            connectionId
                        );
                    }
                },
                error =>
                {
                    _logger.LogError(
                        error,
                        "[ClientHub] Error in message bus subscription for {ConnectionId}",
                        connectionId
                    );
                }
            );

        // Store subscription for cleanup
        _subscriptions[connectionId] = subscription;

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects. Cleans up message bus subscription.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        var username = Context.User?.Identity?.Name ?? "unknown";

        _logger.LogInformation(
            "[ClientHub] User {Username} disconnected with connection ID {ConnectionId}",
            username,
            connectionId
        );

        // Dispose and remove subscription
        if (_subscriptions.TryRemove(connectionId, out var subscription))
        {
            subscription?.Dispose();
        }

        await base.OnDisconnectedAsync(exception);
    }
}

