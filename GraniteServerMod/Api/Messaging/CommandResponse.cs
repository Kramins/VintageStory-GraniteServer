using System;

namespace GraniteServer.Api.Messaging;

/// <summary>
/// Response envelope for commands. Command handlers can publish or return this
/// to indicate success/failure and include optional result data.
/// </summary>
public class CommandResponse : MessageBusMessage
{
    public Guid ParentCommandId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class CommandResponse<T> : CommandResponse
{
    /// <summary>
    /// Strongly-typed result data.
    /// </summary>
    public new T? Data
    {
        get => (T?)base.Data;
        set => base.Data = value;
    }
}