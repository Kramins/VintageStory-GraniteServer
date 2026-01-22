using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Granite.Server.Services;
using GraniteServer.Messaging;
using GraniteServer.Messaging.Commands;
using GraniteServer.Messaging.Events;
using GraniteServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Granite.Server.Hubs;

/// <summary>
/// SignalR Hub for Granite Mod communication
/// </summary>
[Authorize]
public class GraniteHub : Hub
{
    private readonly PersistentMessageBusService _messageBus;
    private readonly ServersService _serversService;
    private readonly ILogger<GraniteHub> _logger;
    private static readonly ConcurrentDictionary<string, IDisposable> _subscriptions = new();

    public GraniteHub(
        PersistentMessageBusService messageBus,
        ILogger<GraniteHub> logger,
        ServersService serversService
    )
    {
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        _serversService = serversService ?? throw new ArgumentNullException(nameof(serversService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Receives an event from a client and publishes it to the server's message bus.
    /// </summary>
    /// <param name="message">The message to publish</param>
    public async Task PublishEvent(JsonElement payload)
    {
        try
        {
            var messageType =
                payload.GetProperty("messageType").GetString()
                ?? throw new InvalidOperationException("messageType is required");

            var type = FindMessageTypeByName(messageType);
            if (type == null)
            {
                _logger.LogError($"[SignalR] Could not find message type: {messageType}");
                throw new InvalidOperationException($"Unknown messageType: {messageType}");
            }

            // Configure JSON options to handle property name case-insensitivity and nested types
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            var message =
                (MessageBusMessage?)JsonSerializer.Deserialize(payload.GetRawText(), type, options)
                ?? throw new InvalidOperationException("Failed to deserialize message");

            var isValid = ValidateMessage(message);
            if (!isValid)
            {
                _logger.LogWarning("[SignalR] Message validation failed");
                throw new UnauthorizedAccessException("Message validation failed");
            }

            _logger.LogTrace(
                $"[SignalR] Publishing message to bus: {message.GetType().FullName}, Data: {message.Data?.GetType().FullName ?? "null"}"
            );

            if (message is EventMessage @event)
            {
                _messageBus.Publish(@event);
            }
            else
            {
                _logger.LogWarning(
                    "[SignalR] Only EventMessage types are supported for publishing"
                );
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SignalR] Error publishing event from client");
            throw;
        }
    }

    private bool ValidateMessage(MessageBusMessage message)
    {
        // Validate that the OriginServerId matches the ServerId claim from the JWT token
        var serverIdClaim = Context.User?.FindFirst("ServerId")?.Value;
        if (
            !string.IsNullOrEmpty(serverIdClaim)
            && Guid.TryParse(serverIdClaim, out var claimedServerId)
        )
        {
            if (message.OriginServerId != claimedServerId)
            {
                _logger.LogWarning(
                    "[SignalR] Rejected event from connection {ConnectionId}: OriginServerId {OriginServerId} does not match claimed ServerId {ClaimedServerId}",
                    Context.ConnectionId,
                    message.OriginServerId,
                    claimedServerId
                );
                return false;
            }
        }
        else
        {
            _logger.LogWarning(
                "[SignalR] Connection {ConnectionId} attempted to publish event without valid ServerId claim",
                Context.ConnectionId
            );
            return false;
        }

        return true;
    }

    private static Type? FindMessageTypeByName(string messageType)
    {
        return AppDomain
            .CurrentDomain.GetAssemblies()
            .SelectMany(assembly =>
            {
                try
                {
                    return assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    return ex.Types.Where(t => t != null)!;
                }
            })
            .FirstOrDefault(t =>
                t != null
                && !t.IsAbstract
                && typeof(MessageBusMessage).IsAssignableFrom(t)
                && t.Name.Equals(messageType, StringComparison.OrdinalIgnoreCase)
            );
    }

    public async Task AcknowledgeCommand(Guid commandId)
    {
        _logger.LogTrace(
            "[SignalR] Acknowledged command {CommandId} from connection {ConnectionId}",
            commandId,
            Context.ConnectionId
        );

        await _messageBus.AcknowledgeCommandAsync(commandId);
    }

  

    /// <summary>
    /// Called when a client connects. Subscribes the client to message bus events.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        var serverIdClaim = Context.User?.FindFirst("ServerId")?.Value;
        if (string.IsNullOrEmpty(serverIdClaim))
        {
            _logger.LogWarning(
                "[SignalR] Connection {ConnectionId} has no ServerId claim, disconnecting",
                connectionId
            );
            Context.Abort();
            return;
        }

        var serverId = Guid.Parse(serverIdClaim!);

        // Mark server as online
        await _serversService.MarkServerOnlineAsync(serverId);

        // Subscribe to all messages from the message bus
        var subscription = _messageBus
            .GetObservable()
            .Where(msg =>
                // Filter messages to only those intended for this server or broadcast messages
                msg.TargetServerId == MessageBusMessage.BroadcastServerId
                || msg.TargetServerId == serverId
            )
            .Subscribe(
                message =>
                {
                    // Broadcast event to this specific client
                    Clients
                        .Client(connectionId)
                        .SendAsync(SignalRHubMethods.ReceiveEvent, message);
                },
                error =>
                {
                    // Log error but don't terminate connection
                    _logger.LogError(
                        error,
                        "[SignalR] Error in message bus subscription for {ConnectionId}",
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

        // Dispose and remove subscription
        if (_subscriptions.TryRemove(connectionId, out var subscription))
        {
            subscription?.Dispose();
        }

        var serverIdClaim = Context.User?.FindFirst("ServerId")?.Value;
        if (!string.IsNullOrEmpty(serverIdClaim))
        {
            var serverId = Guid.Parse(serverIdClaim!);
            // Mark server as offline
            await _serversService.MarkServerOfflineAsync(serverId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
