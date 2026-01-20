using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GraniteServer.Data;
using GraniteServer.Data.Entities;
using GraniteServer.Messaging;
using GraniteServer.Messaging.Commands;
using GraniteServer.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Granite.Server.Services;

/// <summary>
/// Extended MessageBusService that adds database persistence for command buffering and reply handling.
/// Commands are stored in the database before being published, allowing for retrieval and replay.
/// </summary>
public class PersistentMessageBusService : MessageBusService
{
    private readonly GraniteDataContext _dbContext;
    private readonly ILogger<PersistentMessageBusService> _logger;

    public PersistentMessageBusService(
        GraniteDataContext dbContext,
        ILogger<PersistentMessageBusService> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Publishes a command and stores it in the database for persistence
    /// </summary>
    public async Task<Guid> PublishCommandAsync(CommandMessage command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        var commandEntity = new CommandEntity
        {
            Id = command.Id,
            ServerId = command.TargetServerId,
            MessageType = command.GetType().Name,
            Payload = JsonSerializer.Serialize(command),
            CreatedAt = DateTime.UtcNow,
            Status = CommandStatus.Pending,
        };

        _dbContext.BufferedCommands.Add(commandEntity);
        await _dbContext.SaveChangesAsync();

        _logger.LogDebug(
            "Stored command {CommandId} of type {MessageType} for server {ServerId}",
            command.Id,
            command.GetType().Name,
            command.TargetServerId
        );

        // Publish to in-memory bus
        base.Publish(command);

        return commandEntity.Id;
    }

    /// <summary>
    /// Retrieves buffered pending commands for a specific server
    /// </summary>
    public async Task<List<CommandEntity>> GetPendingCommandsAsync(Guid serverId)
    {
        return await _dbContext
            .BufferedCommands.Where(c =>
                c.ServerId == serverId && c.Status == CommandStatus.Pending
            )
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Marks a command as sent
    /// </summary>
    public async Task MarkCommandAsSentAsync(Guid commandId)
    {
        var command = await _dbContext.BufferedCommands.FindAsync(commandId);
        if (command != null)
        {
            command.Status = CommandStatus.Sent;
            command.SentAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            _logger.LogDebug("Marked command {CommandId} as sent", commandId);
        }
    }

    /// <summary>
    /// Stores a command response in the database
    /// </summary>
    public async Task StoreCommandResponseAsync(
        Guid commandId,
        CommandResponse response,
        bool success = true
    )
    {
        var command = await _dbContext.BufferedCommands.FindAsync(commandId);
        if (command != null)
        {
            command.Status = success ? CommandStatus.Completed : CommandStatus.Failed;
            command.ResponsePayload = JsonSerializer.Serialize(response);
            command.ErrorMessage = response.ErrorMessage;
            await _dbContext.SaveChangesAsync();

            _logger.LogDebug(
                "Stored response for command {CommandId} with status {Status}",
                commandId,
                command.Status
            );
        }
    }

    /// <summary>
    /// Retrieves and publishes buffered pending commands for a specific server
    /// </summary>
    public async Task<int> PublishBufferedCommandsAsync(Guid serverId)
    {
        var pendingCommands = await GetPendingCommandsAsync(serverId);

        foreach (var commandEntity in pendingCommands)
        {
            try
            {
                // Deserialize the command based on its type
                var messageType = Type.GetType(commandEntity.MessageType);
                if (messageType != null)
                {
                    var command =
                        JsonSerializer.Deserialize(commandEntity.Payload, messageType)
                        as CommandMessage;

                    if (command != null)
                    {
                        base.Publish(command);
                        await MarkCommandAsSentAsync(commandEntity.Id);

                        _logger.LogInformation(
                            "Published buffered command {CommandId} of type {MessageType} for server {ServerId}",
                            commandEntity.Id,
                            commandEntity.MessageType,
                            serverId
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to publish buffered command {CommandId} for server {ServerId}",
                    commandEntity.Id,
                    serverId
                );

                // Mark as failed
                commandEntity.Status = CommandStatus.Failed;
                commandEntity.ErrorMessage = ex.Message;
                await _dbContext.SaveChangesAsync();
            }
        }

        return pendingCommands.Count;
    }

    /// <summary>
    /// Cleans up old completed/failed commands from the database
    /// </summary>
    public async Task<int> CleanupOldCommandsAsync(TimeSpan olderThan)
    {
        var cutoffDate = DateTime.UtcNow - olderThan;

        var oldCommands = await _dbContext
            .BufferedCommands.Where(c =>
                c.CreatedAt < cutoffDate
                && (c.Status == CommandStatus.Completed || c.Status == CommandStatus.Failed)
            )
            .ToListAsync();

        _dbContext.BufferedCommands.RemoveRange(oldCommands);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Cleaned up {Count} old commands", oldCommands.Count);

        return oldCommands.Count;
    }

    /// <summary>
    /// Acknowledges a command and removes it from the database
    /// </summary>
    public async Task<bool> AcknowledgeCommandAsync(Guid commandId)
    {
        var command = await _dbContext.BufferedCommands.FindAsync(commandId);
        if (command == null)
        {
            _logger.LogWarning(
                "Attempted to acknowledge non-existent command {CommandId}",
                commandId
            );
            return false;
        }

        _dbContext.BufferedCommands.Remove(command);
        await _dbContext.SaveChangesAsync();

        _logger.LogDebug(
            "Acknowledged and deleted command {CommandId} of type {MessageType}",
            commandId,
            command.MessageType
        );

        return true;
    }
}
