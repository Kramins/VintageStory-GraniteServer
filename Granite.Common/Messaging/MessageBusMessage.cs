using System;
using System.Collections.Generic;

namespace GraniteServer.Messaging;

/// <summary>
/// Represents a message (command or event) that can be published through the MessageBus and streamed to clients via SSE.
/// </summary>
public abstract class MessageBusMessage
{
    public static readonly Guid BroadcastServerId = Guid.Empty;
    public Guid Id { get; set; } = Guid.NewGuid();

    public virtual string MessageType => GetType().Name;

    /// <summary>
    /// Destination server for routing. The bus stamps the local server ID when unset.
    /// </summary>
    public Guid TargetServerId { get; set; } = Guid.Empty;

    public Guid OriginServerId { get; set; } = Guid.Empty;

    /// <summary>
    /// W3C Trace-Context parent identifier for distributed tracing.
    /// Future use
    /// </summary>
    public string? TraceParent { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public object? Data { get; set; }
}

public abstract class MessageBusMessage<T> : MessageBusMessage
{
    /// <summary>
    /// Strongly-typed message payload.
    /// </summary>
    public new T? Data
    {
        get => (T?)base.Data;
        set => base.Data = value;
    }
}
