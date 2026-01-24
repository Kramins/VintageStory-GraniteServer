using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using GraniteServer.Messaging;
using GraniteServer.Messaging.Commands;
using GraniteServer.Messaging.Events;

namespace GraniteServer.Services
{
    /// <summary>
    /// Centralized event bus service that allows any service to publish events
    /// and clients to subscribe to events via SSE or other mechanisms.
    ///
    /// Thread-safe singleton using Rx.NET synchronized Subject for broadcast to all subscribers.
    /// </summary>
    public class MessageBusService
    {
        private readonly ISubject<MessageBusMessage> _subject;

        public MessageBusService()
        {
            _subject = Subject.Synchronize(new Subject<MessageBusMessage>());
        }

        /// <summary>
        /// Publishes an event to the bus. This is thread-safe and non-blocking.
        /// All current subscribers will receive the event immediately.
        /// </summary>
        public void Publish(MessageBusMessage @event)
        {
            if (@event == null)
            {
                return;
            }


            try
            {
                _subject.OnNext(@event);
            }
            catch (ObjectDisposedException ex)
            {
                // Log that the subject has been disposed - this indicates the message bus has been shut down
            }
            catch (Exception ex)
            {
                // Silently handle other exceptions to prevent message publishing from blocking
            }
        }


        /// <summary>
        /// Returns an IObservable that can be subscribed to receive events.
        /// Each subscriber will receive all events from the ReplaySubject (broadcast).
        /// New subscribers also receive buffered past events up to ReplayBufferSize.
        /// </summary>
        public IObservable<MessageBusMessage> GetObservable()
        {
            // Only forward EventMessage instances that target this server.
            return _subject
            // .Where(e => e is EventMessage em && em.IssuerServerId == _config.ServerId)
            .AsObservable();
        }

        /// <summary>
        /// Graceful shutdown: completes the subject so no new events can be emitted.
        /// Existing subscribers will complete once all events are consumed.
        /// </summary>
        public void Shutdown()
        {
            try
            {
                _subject.OnCompleted();
            }
            catch (Exception ex) { }
        }

        public T CreateCommand<T>(Guid serverId, Action<T> value)
            where T : CommandMessage
        {
            var command = Activator.CreateInstance<T>();

            // Get the data type from the CommandMessage<TData> base class
            var baseType = typeof(T).BaseType;
            if (
                baseType?.IsGenericType == true
                && baseType.GetGenericTypeDefinition() == typeof(CommandMessage<>)
            )
            {
                var commandDataType = baseType.GetGenericArguments()[0];
                command.Data = Activator.CreateInstance(commandDataType);
            }

            value(command);

            command.TargetServerId = serverId;
            command.Timestamp = DateTime.UtcNow;
            command.TraceParent = Guid.NewGuid().ToString(); // For tracing, could be improved with actual trace IDs

            return command;
        }

        public T CreateEvent<T>(Guid serverId, Action<T> value)
            where T : EventMessage
        {
            var @event = Activator.CreateInstance<T>();

            // Get the data type from the EventMessage<TData> base class
            var baseType = typeof(T).BaseType;
            if (
                baseType?.IsGenericType == true
                && baseType.GetGenericTypeDefinition() == typeof(EventMessage<>)
            )
            {
                var eventDataType = baseType.GetGenericArguments()[0];
                @event.Data = Activator.CreateInstance(eventDataType);
            }

            value(@event);

            @event.OriginServerId = serverId;
            @event.Timestamp = DateTime.UtcNow;
            @event.TraceParent = Guid.NewGuid().ToString(); // For tracing, could be improved with actual trace IDs

            return @event;
        }
    }
}
