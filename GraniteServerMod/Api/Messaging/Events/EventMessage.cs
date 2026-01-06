using System;

namespace GraniteServer.Api.Messaging.Events;

/// <summary>
/// Non-generic event message container with event-specific metadata.
/// </summary>
public class EventMessage : MessageBusMessage { }

/// <summary>
/// Generic event message with typed Data and event-specific metadata.
/// </summary>
public class EventMessage<T> : EventMessage
{
    public new T? Data
    {
        get => (T?)base.Data;
        set => base.Data = value;
    }
}
