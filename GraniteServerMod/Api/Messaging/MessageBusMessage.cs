using System;
using System.Collections.Generic;

namespace GraniteServer.Api.Messaging;

/// <summary>
/// Represents a message (command or event) that can be published through the MessageBus and streamed to clients via SSE.
/// </summary>
public abstract class MessageBusMessage
{
    public static string GetMessageType<T>()
        where T : MessageBusMessage
    {
        return typeof(T).Name;
    }

    /// <summary>
    /// Unique identifier for this message.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Type of the message.
    /// </summary>
    public virtual string MessageType => GetType().Name;

    /// <summary>
    /// The target server ID for this message. Used for filtering messages by destination server.
    /// For events: the server(s) that should receive the event.
    /// For commands: the server that should execute the command.
    /// </summary>
    public Guid TargetServerId { get; set; } = Guid.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Message payload as JSON string or serialized object.
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Arbitrary metadata key/value pairs for routing, debugging or transport hints.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    public override string ToString()
    {
        return $"MessageBusMessage(Id={Id}, Type={MessageType}, Data={Data})";
    }
}

public class MessageBusMessage<T> : MessageBusMessage
{
    /// <summary>
    /// Strongly-typed message data.
    /// </summary>
    public new T? Data
    {
        get => (T?)base.Data;
        set => base.Data = value;
    }
}
