using System;

namespace GraniteServer.Messaging.Commands;

public abstract class CommandMessage : MessageBusMessage
{
    public Guid SourceServerId { get; set; } = Guid.Empty;
}

public abstract class CommandMessage<T> : CommandMessage
{
    public new T? Data
    {
        get => (T?)base.Data;
        set => base.Data = value;
    }
}
