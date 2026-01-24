using System;

namespace GraniteServer.Messaging;

/// <summary>
/// Attribute to mark events that should be propagated to the ClientApp via SignalR.
/// Events without this attribute or with SendToClient=false will only be available server-side.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ClientEventAttribute : Attribute
{
    public ClientEventAttribute(bool sendToClient = true)
    {
        SendToClient = sendToClient;
    }

    /// <summary>
    /// Indicates whether this event should be sent to connected client applications.
    /// </summary>
    public bool SendToClient { get; }
}
