using Granite.Web.Client.Services.Auth;
using GraniteServer.Messaging;
using GraniteServer.Messaging.Events;
using Microsoft.AspNetCore.SignalR.Client;

namespace Granite.Web.Client.Services.SignalR;

/// <summary>
/// SignalR service for real-time communication with the server.
/// </summary>
public class SignalRService : ISignalRService, IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private readonly ILogger<SignalRService> _logger;
    private readonly string _hubUrl;
    private readonly CustomAuthenticationStateProvider _authStateProvider;
    private bool _isConnected;
    private Task? _reconnectTask;
    private CancellationTokenSource _reconnectCancellationTokenSource = new();
    private const int ReconnectDelayMs = 3000;
    private const int MaxReconnectAttempts = 5;
    private int _reconnectAttempts;

    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    public bool IsConnected
    {
        get => _isConnected;
        private set
        {
            if (_isConnected != value)
            {
                _isConnected = value;
                ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(value));
                _logger.LogInformation("SignalR connection state changed: {IsConnected}", value);
            }
        }
    }

    public SignalRService(
        ILogger<SignalRService> logger,
        IConfiguration configuration,
        CustomAuthenticationStateProvider authStateProvider
    )
    {
        _logger = logger;
        _authStateProvider = authStateProvider;
        var apiBaseUrl = configuration["ApiBaseUrl"] ?? "http://localhost:5000";
        _hubUrl = $"{apiBaseUrl}/hub/client";
    }

    /// <summary>
    /// Starts the connection to the SignalR hub with auto-reconnect capability.
    /// </summary>
    public async Task ConnectAsync()
    {
        if (_isConnected || _hubConnection?.State == HubConnectionState.Connected)
        {
            _logger.LogWarning("SignalR connection already established or connecting");
            return;
        }

        try
        {
            // Get the authentication token
            var token = await _authStateProvider.GetTokenAsync();

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(
                    _hubUrl,
                    options =>
                    {
                        if (!string.IsNullOrEmpty(token))
                        {
                            options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                        }
                    }
                )
                .WithAutomaticReconnect(
                    new[]
                    {
                        TimeSpan.Zero,
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(3),
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(10),
                    }
                )
                .WithStatefulReconnect()
                .Build();

            // Register hub method handlers - the server uses "ServerEvent" not "ReceiveEvent"
            _hubConnection.On<EventMessage>(SignalRHubMethods.ReceiveEvent, OnServerEventReceived);

            _hubConnection.Reconnecting += OnReconnecting;
            _hubConnection.Reconnected += OnReconnected;
            _hubConnection.Closed += OnConnectionClosed;

            await _hubConnection.StartAsync();
            IsConnected = true;
            _reconnectAttempts = 0;
            _logger.LogInformation("SignalR connection established successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to establish SignalR connection");
            IsConnected = false;
            throw;
        }
    }

    /// <summary>
    /// Stops the connection to the SignalR hub.
    /// </summary>
    public async Task DisconnectAsync()
    {
        try
        {
            _reconnectCancellationTokenSource.Cancel();

            if (_hubConnection is not null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }

            IsConnected = false;
            _logger.LogInformation("SignalR connection closed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing SignalR connection");
        }
    }

    /// <summary>
    /// Handles server events received from the hub.
    /// </summary>
    private async Task OnServerEventReceived(EventMessage eventData)
    {
        _logger.LogDebug(
            "Event received from server: {EventType}",
            eventData?.GetType().Name ?? "unknown"
        );

        // Event handling can be extended here or delegated to event subscribers
        await Task.CompletedTask;
    }

    private async Task OnReconnecting(Exception? ex)
    {
        IsConnected = false;
        _reconnectAttempts++;

        if (ex != null)
        {
            _logger.LogWarning(
                ex,
                "SignalR reconnecting... (Attempt {Attempt})",
                _reconnectAttempts
            );
        }
        else
        {
            _logger.LogWarning("SignalR reconnecting... (Attempt {Attempt})", _reconnectAttempts);
        }

        await Task.CompletedTask;
    }

    private async Task OnReconnected(string? connectionId)
    {
        IsConnected = true;
        _reconnectAttempts = 0;
        _logger.LogInformation(
            "SignalR connection restored. ConnectionId: {ConnectionId}",
            connectionId
        );
        await Task.CompletedTask;
    }

    private async Task OnConnectionClosed(Exception? ex)
    {
        IsConnected = false;

        if (ex != null)
        {
            _logger.LogWarning(ex, "SignalR connection closed unexpectedly");
        }

        // Attempt manual reconnection if not already attempting automatic reconnection
        if (_reconnectAttempts < MaxReconnectAttempts)
        {
            _reconnectTask = ReconnectWithBackoffAsync();
        }
        else
        {
            _logger.LogError("Max reconnection attempts reached. Giving up.");
        }

        await Task.CompletedTask;
    }

    private async Task ReconnectWithBackoffAsync()
    {
        while (
            _reconnectAttempts < MaxReconnectAttempts
            && !_reconnectCancellationTokenSource.Token.IsCancellationRequested
        )
        {
            try
            {
                await Task.Delay(
                    ReconnectDelayMs * _reconnectAttempts,
                    _reconnectCancellationTokenSource.Token
                );
                await ConnectAsync();
                break;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Reconnection attempt cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Reconnection attempt {Attempt} failed", _reconnectAttempts);
                _reconnectAttempts++;
            }
        }
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await DisconnectAsync();
        _reconnectCancellationTokenSource.Dispose();
    }
}
