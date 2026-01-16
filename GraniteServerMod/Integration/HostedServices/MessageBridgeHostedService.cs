using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraniteServer.Api.Services;
using GraniteServer.Messaging.Commands;
using GraniteServer.Messaging.Events;
using GraniteServer.Messaging.Handlers.Commands;
using GraniteServer.Messaging.Handlers.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace GraniteServer.Api.HostedServices
{
    /// <summary>
    /// Hosted service that bridges Vintagestory server events into the EventBusService.
    /// This allows the web API to expose server events to clients via SSE.
    ///
    /// Also subscribes to command messages and dispatches them to registered handlers.
    ///
    /// Subscribes to:
    /// - PlayerLogin / PlayerDisconnect (player session changes)
    /// - ServerSave (world save events)
    /// - Command messages for processing
    /// - Possible future game-world events
    /// </summary>
    public class MessageBridgeHostedService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly MessageBusService _messageBus;
        private readonly IServiceProvider _serviceProvider;
        private IDisposable? _commandSubscription;
        private IDisposable? _eventSubscription;

        public MessageBridgeHostedService(
            ILogger logger,
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
                _logger.Notification("[EventBridge] Starting event bridge...");

                // Subscribe to command messages and dispatch to handlers
                _commandSubscription = _messageBus
                    .GetObservable()
                    .Where(msg => msg is CommandMessage)
                    .Subscribe(msg =>
                    {
                        var cmd = msg as CommandMessage;
                        HandleCommandMessage(cmd!);
                    });

                // Subscribe to events and dispatch handlers
                _eventSubscription = _messageBus
                    .GetObservable()
                    .Where(msg => msg is EventMessage)
                    .Subscribe(msg =>
                    {
                        var evt = msg as EventMessage;
                        HandleEventMessage(evt!);
                    });

                _logger.Notification(
                    "[EventBridge] Event bridge started, subscribed to command messages"
                );
            }
            catch (Exception ex)
            {
                _logger.Error($"[EventBridge] Error starting event bridge: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        private void HandleEventMessage(EventMessage eventMessage)
        {
            _logger.Notification($"[EventBridge] Received event: {eventMessage.MessageType}");
            var handlerInterfaceType = typeof(IEventHandler<>).MakeGenericType(
                eventMessage.GetType()
            );

            if (eventMessage.Data == null)
            {
                _logger.Error(
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
                        var result = handleMethod!.Invoke(
                            handlerObj,
                            new object[] { eventMessage }
                        );
                        if (result is Task task)
                        {
                            task.GetAwaiter().GetResult();
                        }

                        _logger.Notification(
                            $"[EventBridge] Dispatched event {eventMessage.MessageType} to handler {handlerObj!.GetType().Name}"
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(
                            $"[EventBridge] Error handling event {eventMessage.MessageType} in handler {handlerObj!.GetType().Name}: {ex.Message}"
                        );
                    }
                }
            }
        }

        private void HandleCommandMessage(CommandMessage cmd)
        {
            _logger.Notification($"[EventBridge] Received command: {cmd.MessageType}");
            var handlerInterfaceType = typeof(ICommandHandler<>).MakeGenericType(cmd.GetType());

            if (cmd.Data == null)
            {
                _logger.Error(
                    $"[EventBridge] Command {cmd.MessageType} has no data payload, skipping"
                );
                return;
            }

            using (var scope = _serviceProvider.CreateScope())
            {
                var handlers = scope.ServiceProvider.GetServices(handlerInterfaceType);
                var handleMethod = handlerInterfaceType.GetMethod(
                    nameof(ICommandHandler<CommandMessage>.Handle)
                );

                foreach (var handlerObj in handlers)
                {
                    try
                    {
                        var result = handleMethod!.Invoke(handlerObj, new object[] { cmd });
                        if (result is Task task)
                        {
                            task.GetAwaiter().GetResult();
                        }

                        _logger.Notification(
                            $"[EventBridge] Dispatched command {cmd.MessageType} to handler {handlerObj!.GetType().Name}"
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(
                            $"[EventBridge] Error handling command {cmd.MessageType} in handler {handlerObj!.GetType().Name}: {ex.Message}"
                        );
                    }
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.Notification("[EventBridge] Stopping event bridge...");

                _commandSubscription?.Dispose();

                _logger.Notification("[EventBridge] Event bridge stopped");
            }
            catch (Exception ex)
            {
                _logger.Error($"[EventBridge] Error stopping event bridge: {ex.Message}");
            }

            return Task.CompletedTask;
        }
    }
}
