using System;

namespace GraniteServer.Messaging.Events;

/// <summary>
/// Non-generic event message container with event-specific metadata.
/// </summary>
public abstract class EventMessage : MessageBusMessage { }

/// <summary>
/// Generic event message with typed Data and event-specific metadata.
/// </summary>
public abstract class EventMessage<T> : EventMessage
{
    public new T Data
    {
        get => (T)base.Data!;
        set => base.Data = value;
    }
}
