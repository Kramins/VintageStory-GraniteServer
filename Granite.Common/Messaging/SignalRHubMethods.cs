namespace GraniteServer.Messaging;

/// <summary>
/// Constants for SignalR hub method names shared between server and client.
/// </summary>
public static class SignalRHubMethods
{
    /// <summary>
    /// Hub method name for receiving events from the server.
    /// </summary>
    public const string ReceiveEvent = "ReceiveEvent";

    /// <summary>
    /// Hub method name for publishing events to the server.
    /// </summary>
    public const string PublishEvent = "PublishEvent";

    /// <summary>
    /// Hub method name for acknowledging command receipt.
    /// </summary>
    public const string AcknowledgeCommand = "AcknowledgeCommand";
}
