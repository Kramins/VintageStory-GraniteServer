using System;

namespace GraniteServer.Api.Messaging.Commands;

public class CommandMessage : MessageBusMessage
{
    public Guid SourceServerId { get; set; } = Guid.Empty;
}

public class CommandMessage<T> : CommandMessage
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
