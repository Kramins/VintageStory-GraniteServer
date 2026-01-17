using System.Reactive.Linq;
using GraniteServer.Messaging.Commands;
using GraniteServer.Messaging.Events;
using GraniteServer.Messaging.Handlers.Commands;
using GraniteServer.Messaging.Handlers.Events;
using GraniteServer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GraniteServer.Server.HostedServices;

public class MessageBridgeHostedService : IHostedService
{
    private readonly ILogger<MessageBridgeHostedService> _logger;
    private readonly MessageBusService _messageBus;
    private readonly IServiceProvider _serviceProvider;
    private IDisposable? _eventSubscription;

    public MessageBridgeHostedService(
        ILogger<MessageBridgeHostedService> logger,
        MessageBusService messageBus,
        IServiceProvider serviceProvider
    )
    {
        _logger = logger;
        _messageBus = messageBus;
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("[EventBridge] Starting event bridge...");

            // Subscribe to events from mod and dispatch to handlers
            _eventSubscription = _messageBus
                .GetObservable()
                .Where(msg => msg is EventMessage)
                .Subscribe(
                    msg =>
                    {
                        try
                        {
                            var evt = (EventMessage)msg;
                            HandleEventMessage(evt);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[EventBridge] Exception in message handler");
                        }
                    },
                    error =>
                    {
                        _logger.LogError(error, "[EventBridge] Error in message bus subscription");
                    },
                    () =>
                    {
                        _logger.LogInformation("[EventBridge] Message bus subscription completed");
                    }
                );

            _logger.LogInformation(
                "[EventBridge] Event bridge started, subscribed to event messages"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError($"[EventBridge] Error starting event bridge: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    private void HandleEventMessage(EventMessage eventMessage)
    {
        _logger.LogInformation($"[EventBridge] Received event: {eventMessage.MessageType}");
        var handlerInterfaceType = typeof(IEventHandler<>).MakeGenericType(eventMessage.GetType());

        if (eventMessage.Data == null)
        {
            _logger.LogError(
                $"[EventBridge] Event {eventMessage.MessageType} has no data payload, skipping"
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
                        task.GetAwaiter().GetResult();
                    }

                    _logger.LogTrace(
                        $"[EventBridge] Dispatched event {eventMessage.MessageType} to handler {handlerObj!.GetType().Name}"
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        $"[EventBridge] Error handling event {eventMessage.MessageType} in handler {handlerObj!.GetType().Name}: {ex.Message}"
                    );
                }
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("[EventBridge] Stopping event bridge...");

            _eventSubscription?.Dispose();

            _logger.LogInformation("[EventBridge] Event bridge stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError($"[EventBridge] Error stopping event bridge: {ex.Message}");
        }

        return Task.CompletedTask;
    }
}
