using System;

namespace GraniteServer.Data.Entities;

/// <summary>
/// Represents a buffered command in the database for persistence and reply handling
/// </summary>
public class CommandEntity
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// The server this command is targeted to or originated from
    /// </summary>
    public Guid ServerId { get; set; }
    
    /// <summary>
    /// The message type (e.g., "KickPlayerCommand", "BroadcastMessageCommand")
    /// </summary>
    public string MessageType { get; set; } = null!;
    
    /// <summary>
    /// Serialized JSON payload of the command
    /// </summary>
    public string Payload { get; set; } = null!;
    
    /// <summary>
    /// When the command was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// When the command was sent/processed (null if not yet sent)
    /// </summary>
    public DateTime? SentAt { get; set; }
    
    /// <summary>
    /// Status of the command (Pending, Sent, Completed, Failed)
    /// </summary>
    public CommandStatus Status { get; set; }
    
    /// <summary>
    /// Optional response payload (JSON) from the command execution
    /// </summary>
    public string? ResponsePayload { get; set; }
    
    /// <summary>
    /// Error message if command failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Navigation property to the server
    /// </summary>
    public ServerEntity Server { get; set; } = null!;
}

public enum CommandStatus
{
    Pending = 0,
    Sent = 1,
    Completed = 2,
    Failed = 3
}
