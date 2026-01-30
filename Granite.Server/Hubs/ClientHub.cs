using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using Granite.Server.Services;
using GraniteServer.Messaging;
using GraniteServer.Messaging.Events;
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
    private readonly IHubContext<ClientHub> _hubContext;
    private readonly ILogger<ClientHub> _logger;
    private static readonly ConcurrentDictionary<string, IDisposable> _subscriptions = new();

    public ClientHub(
        PersistentMessageBusService messageBus,
        IHubContext<ClientHub> hubContext,
        ILogger<ClientHub> logger
    )
    {
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
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
        // Filter to only events marked with [ClientEvent] attribute
        var subscription = _messageBus
            .GetObservable()
            .Where(m => m is EventMessage && ShouldSendToClient(m.GetType()))
            .Subscribe(
                message =>
                {
                    try
                    {
                        // Broadcast event to this specific client using HubContext
                        _hubContext
                            .Clients.Client(connectionId)
                            .SendAsync(SignalRHubMethods.ReceiveEvent, message);
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
    /// Determines if an event should be sent to client apps based on ClientEvent attribute.
    /// </summary>
    private static bool ShouldSendToClient(Type messageType)
    {
        var attribute = (ClientEventAttribute?)
            Attribute.GetCustomAttribute(messageType, typeof(ClientEventAttribute));

        // Default to false if no attribute is present (explicit opt-in)
        return attribute?.SendToClient ?? false;
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
