using System.Reactive.Linq;
using Granite.Web.Client.Services;
using GraniteServer.Messaging.Events;
using GraniteServer.Messaging.Handlers.Events;

namespace Granite.Web.Client.HostedServices;

/// <summary>
/// Client-side service that bridges incoming events from the message bus
/// to registered event handlers. Mirrors the server-side MessageBridgeHostedService pattern.
/// Note: Blazor WebAssembly doesn't support IHostedService, so this uses manual initialization.
/// </summary>
public class MessageBridgeService : IAsyncDisposable
{
    private readonly ILogger<MessageBridgeService> _logger;
    private readonly ClientMessageBusService _messageBus;
    private readonly IServiceProvider _serviceProvider;
    private IDisposable? _eventSubscription;
    private bool _isStarted;

    public MessageBridgeService(
        ILogger<MessageBridgeService> logger,
        ClientMessageBusService messageBus,
        IServiceProvider serviceProvider
    )
    {
        _logger = logger;
        _messageBus = messageBus;
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync()
    {
        // If we start tracking subscription filters this will have to be refactored
        if (_isStarted)
        {
            _logger.LogWarning("[ClientEventBridge] Event bridge already started");
            return Task.CompletedTask;
        }

        try
        {
            _logger.LogInformation("[ClientEventBridge] Starting client event bridge...");

            // Subscribe to events from the message bus and dispatch to handlers
            _eventSubscription = _messageBus
                .GetObservable()
                .Where(msg => msg is EventMessage)
                .SelectMany(async msg =>
                {
                    try
                    {
                        var evt = (EventMessage)msg;
                        await HandleEventMessageAsync(evt);
                        return msg;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[ClientEventBridge] Exception in message handler");
                        return msg;
                    }
                })
                .Subscribe(
                    _ => { },
                    error =>
                    {
                        _logger.LogError(
                            error,
                            "[ClientEventBridge] Error in message bus subscription"
                        );
                    },
                    () =>
                    {
                        _logger.LogInformation(
                            "[ClientEventBridge] Message bus subscription completed"
                        );
                    }
                );

            _isStarted = true;
            _logger.LogInformation(
                "[ClientEventBridge] Client event bridge started, subscribed to event messages"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ClientEventBridge] Error starting client event bridge");
        }

        return Task.CompletedTask;
    }

    private async Task HandleEventMessageAsync(EventMessage eventMessage)
    {
        _logger.LogDebug(
            "[ClientEventBridge] Received event: {EventType}",
            eventMessage.MessageType
        );

        var handlerInterfaceType = typeof(IEventHandler<>).MakeGenericType(
            eventMessage.GetType()
        );

        if (eventMessage.Data == null)
        {
            _logger.LogError(
                "[ClientEventBridge] Event {EventType} has no data payload, skipping",
                eventMessage.MessageType
            );
            return;
        }

        using (var scope = _serviceProvider.CreateScope())
        {
            var handlers = scope.ServiceProvider.GetServices(handlerInterfaceType);
            var handleMethod = handlerInterfaceType.GetMethod(
                nameof(IEventHandler<EventMessage>.Handle)
            );

            foreach (var handlerObj in handlers)
            {
                try
                {
                    var result = handleMethod!.Invoke(handlerObj, new object[] { eventMessage });
                    if (result is Task task)
                    {
                        await task;
                    }

                    _logger.LogTrace(
                        "[ClientEventBridge] Dispatched event {EventType} to handler {HandlerType}",
                        eventMessage.MessageType,
                        handlerObj!.GetType().Name
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "[ClientEventBridge] Error handling event {EventType} in handler {HandlerType}",
                        eventMessage.MessageType,
                        handlerObj!.GetType().Name
                    );
                }
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            _logger.LogInformation("[ClientEventBridge] Stopping client event bridge...");

            _eventSubscription?.Dispose();
            _isStarted = false;

            _logger.LogInformation("[ClientEventBridge] Client event bridge stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ClientEventBridge] Error stopping client event bridge");
        }

        await Task.CompletedTask;
    }
}
