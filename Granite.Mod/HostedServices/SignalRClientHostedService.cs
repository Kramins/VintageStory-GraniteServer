using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Granite.Common.Dto;
using GraniteServer.Messaging;
using GraniteServer.Messaging.Commands;
using GraniteServer.Mod;
using GraniteServer.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Vintagestory.API.Common;

namespace GraniteServer.HostedServices;

/// <summary>
/// Hosted service that maintains a SignalR connection to the Granite.Server hub.
/// Routes incoming messages from the hub to the local ClientMessageBusService.
/// </summary>
public class SignalRClientHostedService : IHostedService, IDisposable
{
    private readonly ILogger _logger;
    private readonly ClientMessageBusService _messageBus;
    private readonly GraniteModConfig _config;
    private readonly SignalRConnectionState _connectionState;
    private readonly HttpClient _httpClient;
    private HubConnection? _hubConnection;
    private CancellationTokenSource? _reconnectCts;
    private IDisposable? _messageBusSubscription;
    private string? _jwtToken;

    public SignalRClientHostedService(
        ILogger logger,
        ClientMessageBusService messageBus,
        GraniteModConfig config,
        SignalRConnectionState connectionState
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _connectionState =
            connectionState ?? throw new ArgumentNullException(nameof(connectionState));
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Starts the SignalR client service. Validates configuration and initiates connection to the server.
    /// If connection fails, starts the reconnection loop in the background.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.Notification("[SignalR] Starting SignalR client service...");

        if (string.IsNullOrWhiteSpace(_config.AccessToken))
        {
            _logger.Warning(
                "[SignalR] AccessToken is not configured. SignalR client will not connect."
            );
            _connectionState.SetConnected(false);
            return;
        }

        try
        {
            await ConnectAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"[SignalR] Failed to start SignalR connection: {ex.Message}");
            // Start reconnection loop
            _ = ReconnectLoopAsync();
        }
    }

    /// <summary>
    /// Stops the SignalR client service. Cancels reconnection attempts, disposes subscriptions, and closes the connection.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.Notification("[SignalR] Stopping SignalR client service...");

        _reconnectCts?.Cancel();
        _reconnectCts?.Dispose();
        _reconnectCts = null;

        _messageBusSubscription?.Dispose();
        _messageBusSubscription = null;

        if (_hubConnection != null)
        {
            try
            {
                await _hubConnection.StopAsync(cancellationToken);
                _logger.Notification("[SignalR] Connection stopped.");
            }
            catch (Exception ex)
            {
                _logger.Warning($"[SignalR] Error stopping connection: {ex.Message}");
            }
        }
    }

    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        await ExchangeAccessTokenForJwtAsync(cancellationToken);
        BuildHubConnection();
        RegisterHubEventHandlers(cancellationToken);
        await StartHubConnectionAsync(cancellationToken);
        SubscribeToLocalMessageBus();
    }

    /// <summary>
    /// Exchanges the server's access token for a JWT token from the authentication endpoint.
    /// </summary>
    private async Task ExchangeAccessTokenForJwtAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.Notification("[SignalR] Exchanging access token for JWT...");
            var tokenUrl = $"{_config.GraniteServerHost.TrimEnd('/')}/api/auth/token";

            var response = await _httpClient.PostAsJsonAsync(
                tokenUrl,
                new AccessTokenRequestDTO
                {
                    ServerId = _config.ServerId,
                    AccessToken = _config.AccessToken!,
                },
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new Exception(
                    $"Failed to exchange access token: {response.StatusCode} - {error}"
                );
            }

            var tokenDto = await response.Content.ReadFromJsonAsync<TokenDTO>(cancellationToken);
            if (tokenDto == null || string.IsNullOrWhiteSpace(tokenDto.AccessToken))
            {
                throw new Exception("Received empty JWT token from server");
            }

            _jwtToken = tokenDto.AccessToken;
            _logger.Notification("[SignalR] JWT token obtained successfully.");
        }
        catch (Exception ex)
        {
            _logger.Error($"[SignalR] Failed to obtain JWT token: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Builds and initializes the SignalR hub connection with authentication and reconnection settings.
    /// </summary>
    private void BuildHubConnection()
    {
        var hubUrl = $"{_config.GraniteServerHost.TrimEnd('/')}{_config.HubPath}";
        _logger.Notification($"[SignalR] Connecting to hub at {hubUrl}");

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(
                hubUrl,
                options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(_jwtToken)!;
                    // Increase buffer size for large messages (e.g., collectibles sync with 17k items)
                    // Default is 32KB, increase to 10MB
                    options.ApplicationMaxBufferSize = 10 * 1024 * 1024; // 10MB
                    options.TransportMaxBufferSize = 10 * 1024 * 1024; // 10MB
                }
            )
            .WithAutomaticReconnect(
                _config.ReconnectDelaysSeconds.Select(s => TimeSpan.FromSeconds(s)).ToArray()
            )
            .Build();
    }

    /// <summary>
    /// Registers message handlers for incoming events from the server hub.
    /// </summary>
    private void RegisterHubEventHandlers(CancellationToken cancellationToken)
    {
        if (_hubConnection == null)
            return;

        // Register handler for receiving events from the server
        _hubConnection.On<System.Text.Json.JsonElement>(
            SignalRHubMethods.ReceiveEvent,
            OnReceiveEventAsync
        );

        // Handle disconnection and reconnection events

        _hubConnection.Reconnecting += error => OnHubReconnecting(error);

        _hubConnection.Reconnected += connectionId => OnHubReconnected(connectionId);

        _hubConnection.Closed += error => OnHubClosed(error, cancellationToken);
    }

    /// <summary>
    /// Starts the SignalR hub connection and sets the connection state.
    /// </summary>
    private async Task StartHubConnectionAsync(CancellationToken cancellationToken)
    {
        await _hubConnection!.StartAsync(cancellationToken);
        _logger.Notification(
            $"[SignalR] Connected successfully. ConnectionId: {_hubConnection.ConnectionId}"
        );
        _connectionState.SetConnected(true);
    }

    /// <summary>
    /// Subscribes to the local message bus and forwards events to the server via SignalR.
    /// Handles errors and attempts recovery without terminating the subscription.
    /// </summary>
    private void SubscribeToLocalMessageBus()
    {
        _messageBusSubscription = _messageBus
            .GetObservable()
            .Where(msg =>
                // Only forward messages that originated from this server
                // Don't echo back messages received from the control plane
                msg.OriginServerId == _config.ServerId
            )
            .Subscribe(
                message => _ = HandleMessageAsync(message),
                error =>
                {
                    _logger.Error(
                        $"[SignalR] Fatal error in MessageBus subscription: {error.Message}"
                    );
                },
                () =>
                {
                    _logger.Warning("[SignalR] MessageBus subscription completed");
                }
            );
    }

    /// <summary>
    /// Handles the hub reconnection event. Marks the server as offline during disconnection.
    /// </summary>
    private Task OnHubReconnecting(Exception? error)
    {
        _logger.Warning($"[SignalR] Connection lost. Reconnecting... ({error?.Message})");
        _connectionState.SetConnected(false);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the hub reconnected event. Marks server as online.
    /// </summary>
    private Task OnHubReconnected(string? connectionId)
    {
        _logger.Notification($"[SignalR] Reconnected successfully. ConnectionId: {connectionId}");
        _connectionState.SetConnected(true);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the hub closed event. Marks server as offline and initiates reconnection loop.
    /// </summary>
    private async Task OnHubClosed(Exception? error, CancellationToken cancellationToken)
    {
        _logger.Error($"[SignalR] Connection closed: {error?.Message}");
        _connectionState.SetConnected(false);
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        _ = ReconnectLoopAsync();
    }

    private void OnReceiveEventAsync(JsonElement payload)
    {
        try
        {
            var messageType =
                payload.GetProperty("messageType").GetString()
                ?? throw new InvalidOperationException("messageType is required");

            var type = FindMessageTypeByName(messageType);
            if (type == null)
            {
                _logger.Error($"[SignalR] Could not find message type: {messageType}");
                return;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            var message =
                (MessageBusMessage?)JsonSerializer.Deserialize(payload.GetRawText(), type, options)
                ?? throw new InvalidOperationException("Failed to deserialize message");

            if (message.OriginServerId == _config.ServerId)
            {
                _logger.Debug($"[SignalR] Ignoring own message: {message.MessageType}");
                return;
            }

            _logger.Debug($"[SignalR] Received event: {message.MessageType}");
            _messageBus.Publish(message);

            if (message is CommandMessage commandMessage)
            {
                _hubConnection!.InvokeAsync(
                    SignalRHubMethods.AcknowledgeCommand,
                    commandMessage.Id,
                    CancellationToken.None
                );
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"[SignalR] Error processing received event: {ex.Message}");
        }
    }

    private Type? FindMessageTypeByName(string messageType)
    {
        return AppDomain
            .CurrentDomain.GetAssemblies()
            .SelectMany(assembly =>
            {
                try
                {
                    return assembly.GetTypes();
                }
                catch (System.Reflection.ReflectionTypeLoadException ex)
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

    /// <summary>
    /// Processes messages that were queued while the connection was inactive.
    /// Attempts to send all queued messages in order after reconnection.
    
    /// <summary>
    /// Handles a message from the local message bus and sends it to the server via SignalR.
    /// Drops the message if the connection is not currently active.
    /// </summary>
    private async Task HandleMessageAsync(MessageBusMessage message)
    {
        try
        {
            // If not connected, drop the message (don't queue)
            if (_hubConnection?.State != HubConnectionState.Connected)
            {
                _logger.Debug(
                    $"[SignalR] Connection not active. Dropping event: {message.MessageType}"
                );
                return;
            }

            // Log message size for debugging
            var json = JsonSerializer.Serialize(message);
            var sizeKB = json.Length / 1024.0;
            _logger.Debug($"[SignalR] Sending event to server: {message.MessageType} (Size: {sizeKB:F2} KB)");
            
            if (sizeKB > 100)
            {
                _logger.Warning($"[SignalR] Large message detected: {message.MessageType} is {sizeKB:F2} KB");
            }

            await _hubConnection.InvokeAsync(
                SignalRHubMethods.PublishEvent,
                message,
                CancellationToken.None
            );
        }
        catch (Exception ex)
        {
            _logger.Error($"[SignalR] Error sending event to server: {ex.Message}");
            // Drop the message on error instead of queuing
        }
    }

    /// <summary>
    /// Attempts to reconnect to the server hub with exponential backoff delays.
    /// Continues retrying indefinitely until a successful connection is established or cancellation is requested.
    /// </summary>
    private async Task ReconnectLoopAsync()
    {
        _reconnectCts?.Cancel();
        _reconnectCts = new CancellationTokenSource();
        var cancellationToken = _reconnectCts.Token;

        var delayIndex = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var delay =
                    delayIndex < _config.ReconnectDelaysSeconds.Length
                        ? _config.ReconnectDelaysSeconds[delayIndex]
                        : _config.ReconnectDelaysSeconds[^1];

                _logger.Notification($"[SignalR] Attempting reconnect in {delay} seconds...");
                await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);

                await ConnectAsync(cancellationToken);
                _logger.Notification("[SignalR] Reconnection successful.");
                return; // Success, exit loop
            }
            catch (Exception ex)
            {
                _logger.Error($"[SignalR] Reconnection attempt failed: {ex.Message}");
                delayIndex = Math.Min(delayIndex + 1, _config.ReconnectDelaysSeconds.Length - 1);
            }
        }
    }

    public void Dispose()
    {
        _messageBusSubscription?.Dispose();
        _reconnectCts?.Dispose();
        _httpClient?.Dispose();
        _hubConnection?.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(5));
    }
}
