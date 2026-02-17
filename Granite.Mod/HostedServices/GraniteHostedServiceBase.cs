using System.Reactive.Linq;
using GraniteServer.Messaging;
using GraniteServer.Services;
using Microsoft.Extensions.Hosting;
using Vintagestory.API.Common;

namespace GraniteServer.HostedServices;

/// <summary>
/// Base class for all Granite hosted services.
/// Provides common infrastructure including logging helpers, lifecycle management,
/// and optional command subscription helpers for message bus integration.
/// </summary>
public abstract class GraniteHostedServiceBase : IHostedService, IDisposable
{
    protected readonly ClientMessageBusService MessageBus;
    protected readonly ILogger Logger;
    protected readonly string ComponentName;

    private readonly List<IDisposable> _subscriptions = new();
    private bool _disposed;

    protected GraniteHostedServiceBase(
        ClientMessageBusService messageBus,
        ILogger logger)
    {
        MessageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Derive component name from class name (remove "HostedService" suffix if present)
        ComponentName = GetType().Name.Replace("HostedService", "");
    }

    public abstract Task StartAsync(CancellationToken cancellationToken);

    public virtual Task StopAsync(CancellationToken cancellationToken)
    {
        LogNotification("Stopping service...");
        
        DisposeSubscriptions();
        
        LogNotification("Service stopped");
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        DisposeSubscriptions();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    #region Logging Helpers

    /// <summary>
    /// Logs a notification message with the component name prefix.
    /// </summary>
    protected void LogNotification(string message)
    {
        Logger.Notification($"[{ComponentName}] {message}");
    }

    /// <summary>
    /// Logs a warning message with the component name prefix.
    /// </summary>
    protected void LogWarning(string message)
    {
        Logger.Warning($"[{ComponentName}] {message}");
    }

    /// <summary>
    /// Logs an error message with the component name prefix.
    /// </summary>
    protected void LogError(string message)
    {
        Logger.Error($"[{ComponentName}] {message}");
    }

    /// <summary>
    /// Logs a debug message with the component name prefix.
    /// </summary>
    protected void LogDebug(string message)
    {
        Logger.Debug($"[{ComponentName}] {message}");
    }

    #endregion

    #region Command Subscription Helpers

    /// <summary>
    /// Subscribe to a command with a synchronous handler.
    /// </summary>
    protected void SubscribeToCommand<TCommand>(Action<TCommand> handler)
        where TCommand : MessageBusMessage
    {
        var subscription = MessageBus
            .GetObservable()
            .Where(msg => msg is TCommand)
            .Subscribe(msg =>
            {
                try
                {
                    var command = (TCommand)msg;
                    handler(command);
                }
                catch (Exception ex)
                {
                    LogError($"Error handling {typeof(TCommand).Name}: {ex.Message}");
                }
            });

        _subscriptions.Add(subscription);
    }

    /// <summary>
    /// Subscribe to a command with an asynchronous handler.
    /// </summary>
    protected void SubscribeToCommand<TCommand>(System.Func<TCommand, Task> asyncHandler)
        where TCommand : MessageBusMessage
    {
        var subscription = MessageBus
            .GetObservable()
            .Where(msg => msg is TCommand)
            .Subscribe(msg =>
            {
                try
                {
                    var command = (TCommand)msg;
                    // Use GetAwaiter().GetResult() to handle async in Subscribe context
                    // This is acceptable here since we're in a fire-and-forget subscription
                    asyncHandler(command).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    LogError($"Error handling {typeof(TCommand).Name}: {ex.Message}");
                }
            });

        _subscriptions.Add(subscription);
    }

    #endregion

    private void DisposeSubscriptions()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription?.Dispose();
        }
        _subscriptions.Clear();
    }
}
