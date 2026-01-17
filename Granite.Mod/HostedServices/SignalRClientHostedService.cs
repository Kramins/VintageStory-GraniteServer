using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Granite.Common.Dto;
using GraniteServer.Messaging;
using GraniteServer.Mod;
using GraniteServer.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Vintagestory.API.Common;

namespace GraniteServer.HostedServices;

/// <summary>
/// Hosted service that maintains a SignalR connection to the Granite.Server hub.
/// Routes incoming messages from the hub to the local MessageBusService.
/// </summary>
public class SignalRClientHostedService : IHostedService, IDisposable
{
    private readonly ILogger _logger;
    private readonly MessageBusService _messageBus;
    private readonly GraniteModConfig _config;
    private readonly HttpClient _httpClient;
    private readonly Queue<MessageBusMessage> _messageQueue = new();
    private HubConnection? _hubConnection;
    private CancellationTokenSource? _reconnectCts;
    private IDisposable? _messageBusSubscription;
    private string? _jwtToken;

    public SignalRClientHostedService(
        ILogger logger,
        MessageBusService messageBus,
        GraniteModConfig config
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _httpClient = new HttpClient();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.Notification("[SignalR] Starting SignalR client service...");

        if (string.IsNullOrWhiteSpace(_config.AccessToken))
        {
            _logger.Warning(
                "[SignalR] AccessToken is not configured. SignalR client will not connect."
            );
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
        // First, exchange access token for JWT
        try
        {
            _logger.Notification("[SignalR] Exchanging access token for JWT...");
            var tokenUrl = $"{_config.GraniteServerHost.TrimEnd('/')}/api/auth/token";

            var response = await _httpClient.PostAsJsonAsync(
                tokenUrl,
                new AccessTokenRequestDTO { AccessToken = _config.AccessToken! },
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

        // Now connect to SignalR with the JWT token
        var hubUrl = $"{_config.GraniteServerHost.TrimEnd('/')}{_config.HubPath}";
        _logger.Notification($"[SignalR] Connecting to hub at {hubUrl}");

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(
                hubUrl,
                options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(_jwtToken)!;
                }
            )
            .WithAutomaticReconnect(
                _config.ReconnectDelaysSeconds.Select(s => TimeSpan.FromSeconds(s)).ToArray()
            )
            .Build();

        // Register handlers for incoming messages
        _hubConnection.On<MessageBusMessage>(
            SignalRHubMethods.ReceiveEvent,
            message =>
            {
                try
                {
                    _logger.Debug($"[SignalR] Received event: {message.MessageType}");
                    _messageBus.Publish(message);
                }
                catch (Exception ex)
                {
                    _logger.Error($"[SignalR] Error processing received event: {ex.Message}");
                }
            }
        );

        // Handle reconnection events
        _hubConnection.Reconnecting += error =>
        {
            _logger.Warning($"[SignalR] Connection lost. Reconnecting... ({error?.Message})");
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += connectionId =>
        {
            _logger.Notification(
                $"[SignalR] Reconnected successfully. ConnectionId: {connectionId}"
            );
            // Process queued messages after reconnection
            _ = ProcessQueuedMessagesAsync();
            return Task.CompletedTask;
        };

        _hubConnection.Closed += async error =>
        {
            _logger.Error($"[SignalR] Connection closed: {error?.Message}");
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            _ = ReconnectLoopAsync();
        };

        await _hubConnection.StartAsync(cancellationToken);
        _logger.Notification(
            $"[SignalR] Connected successfully. ConnectionId: {_hubConnection.ConnectionId}"
        );

        // Subscribe to local MessageBus and forward events to server
        _messageBusSubscription = _messageBus
            .GetObservable()
            .Subscribe(
                async message =>
                {
                    try
                    {
                        // If not connected, queue the message
                        if (_hubConnection?.State != HubConnectionState.Connected)
                        {
                            _logger.Debug(
                                $"[SignalR] Connection not active. Queueing event: {message.MessageType}"
                            );
                            _messageQueue.Enqueue(message);
                            return;
                        }

                        _logger.Debug($"[SignalR] Sending event to server: {message.MessageType}");
                        await _hubConnection.InvokeAsync(
                            SignalRHubMethods.PublishEvent,
                            message,
                            cancellationToken
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"[SignalR] Error sending event to server: {ex.Message}");
                        // Queue the message if sending failed
                        _messageQueue.Enqueue(message);
                    }
                },
                error =>
                {
                    _logger.Error($"[SignalR] Error in MessageBus subscription: {error.Message}");
                }
            );
    }

    private async Task ProcessQueuedMessagesAsync()
    {
        _logger.Notification($"[SignalR] Processing {_messageQueue.Count} queued messages...");
        
        while (_messageQueue.Count > 0 && _hubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                if (!_messageQueue.TryDequeue(out var message))
                {
                    break;
                }

                _logger.Debug($"[SignalR] Sending queued event: {message.MessageType}");
                await _hubConnection.InvokeAsync(SignalRHubMethods.PublishEvent, message);
            }
            catch (Exception ex)
            {
                _logger.Error($"[SignalR] Error processing queued message: {ex.Message}");
                // Stop processing if an error occurs, remaining messages stay in queue
                break;
            }
        }

        if (_messageQueue.Count > 0)
        {
            _logger.Notification($"[SignalR] {_messageQueue.Count} messages still queued.");
        }
        else
        {
            _logger.Notification("[SignalR] All queued messages processed successfully.");
        }
    }

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
