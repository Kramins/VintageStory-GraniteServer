using System;
using System.Collections.Generic;
using GraniteServer.Messaging;
using GraniteServer.Messaging.Commands;
using Vintagestory.API.Common;

namespace GraniteServer.Services;

/// <summary>
/// Client-side message bus with command deduplication to prevent duplicate command execution.
/// Maintains a circular buffer of recently processed command IDs.
/// </summary>
public class ClientMessageBusService : MessageBusService
{
    private readonly ILogger _logger;
    private readonly int _maxHistorySize;
    private readonly Queue<Guid> _commandQueue;
    private readonly HashSet<Guid> _processedCommands;
    private readonly object _lock = new();

    public ClientMessageBusService(ILogger logger, int maxHistorySize = 1000)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (maxHistorySize <= 0)
            throw new ArgumentException(
                "Max history size must be positive",
                nameof(maxHistorySize)
            );

        _maxHistorySize = maxHistorySize;
        _commandQueue = new Queue<Guid>(maxHistorySize);
        _processedCommands = new HashSet<Guid>();
    }

    /// <summary>
    /// Publishes a message. Commands are deduplicated to prevent duplicate execution.
    /// Publishing happens on the ThreadPool to avoid blocking the game thread.
    /// </summary>
    public new void Publish(MessageBusMessage message)
    {
        if (message is CommandMessage commandMessage)
        {
            lock (_lock)
            {
                // Check if command was already processed
                if (_processedCommands.Contains(commandMessage.Id))
                {
                    _logger.Warning(
                        $"[MessageBus] Skipping duplicate command {commandMessage.Id} of type {commandMessage.MessageType}"
                    );
                    return;
                }

                // Mark as processed before publishing to prevent race conditions
                MarkCommandProcessed(commandMessage.Id);
            }
        }

        // Publish to subscribers on ThreadPool to avoid blocking the game thread
        System.Threading.ThreadPool.QueueUserWorkItem(_ => base.Publish(message), null);
    }

    /// <summary>
    /// Marks a command as processed. Evicts oldest command if buffer is full.
    /// </summary>
    private void MarkCommandProcessed(Guid commandId)
    {
        // Evict oldest if at capacity
        if (_commandQueue.Count >= _maxHistorySize)
        {
            var oldestId = _commandQueue.Dequeue();
            _processedCommands.Remove(oldestId);

            _logger.Debug(
                $"[MessageBus] Command history buffer full. Evicted oldest command {oldestId}"
            );
        }

        // Add new command
        _commandQueue.Enqueue(commandId);
        _processedCommands.Add(commandId);

        _logger.Debug($"[MessageBus] Command {commandId} marked as processed");
    }

    /// <summary>
    /// Gets the current number of tracked commands.
    /// </summary>
    public int TrackedCommandCount
    {
        get
        {
            lock (_lock)
            {
                return _processedCommands.Count;
            }
        }
    }

    /// <summary>
    /// Clears all tracked commands (useful for testing or reset scenarios).
    /// </summary>
    public void ClearCommandHistory()
    {
        lock (_lock)
        {
            _commandQueue.Clear();
            _processedCommands.Clear();
            _logger.Notification("[MessageBus] Command history cleared");
        }
    }
}
