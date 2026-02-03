namespace Granite.Web.Client.Services.SignalR;

/// <summary>
/// Interface for SignalR service providing real-time communication with the server.
/// </summary>
public interface ISignalRService
{
    /// <summary>
    /// Starts the connection to the SignalR hub.
    /// </summary>
    Task ConnectAsync();

    /// <summary>
    /// Stops the connection to the SignalR hub.
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Gets the current connection state.
    /// </summary>
    bool IsConnected { get; }



    /// <summary>
    /// Connection state changed event.
    /// </summary>
    event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
}

/// <summary>
/// Event arguments for connection state changes.
/// </summary>
public class ConnectionStateChangedEventArgs : EventArgs
{
    public ConnectionStateChangedEventArgs(bool isConnected, string? message = null)
    {
        IsConnected = isConnected;
        Message = message;
    }

    public bool IsConnected { get; }
    public string? Message { get; }
}
