using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using GraniteServer.Messaging;
using GraniteServer.Messaging.Commands;

namespace GraniteServer.Services
{
    /// <summary>
    /// Centralized event bus service that allows any service to publish events
    /// and clients to subscribe to events via SSE or other mechanisms.
    ///
    /// Thread-safe singleton using Rx.NET synchronized ReplaySubject for broadcast to all subscribers.
    /// </summary>
    public class MessageBusService
    {
        private readonly ISubject<MessageBusMessage> _subject;

        /// <summary>
        /// Capacity of the replay buffer. Stores up to this many events for new subscribers to catch up.
        /// </summary>
        private const int ReplayBufferSize = 1000;

        public MessageBusService()
        {
            _subject = Subject.Synchronize(new ReplaySubject<MessageBusMessage>(ReplayBufferSize));
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

            // Enrich event with server IDs if not set
            // if (@event.TargetServerId == Guid.Empty)
            // {
            //     @event.TargetServerId = _config.ServerId;
            // }
            // if (@event.OriginServerId == Guid.Empty)
            // {
            //     @event.OriginServerId = _config.ServerId;
            // }

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

        public async Task<CommandResponse<TResponse>> PublishCommandAndWait<TCommand, TResponse>(
            CommandMessage<TCommand> command
        )
        {
            var tcs = new TaskCompletionSource<CommandResponse<TResponse>>();
            _subject
                .Where(msg =>
                    msg is CommandResponse<TResponse> response
                    && response.ParentCommandId == command.Id
                )
                .Take(1)
                .Timeout(TimeSpan.FromSeconds(30))
                .Catch<MessageBusMessage, TimeoutException>(ex =>
                {
                    var timeoutResponse = new CommandResponse<TResponse>()
                    {
                        ParentCommandId = command.Id,
                        Success = false,
                        ErrorMessage = "Command timed out waiting for response",
                    };
                    return Observable.Return(timeoutResponse);
                })
                .Subscribe(responseMsg =>
                {
                    var response = (CommandResponse<TResponse>)responseMsg;
                    tcs.SetResult(response);
                });
            this.Publish(command);
            return await tcs.Task;
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
    }
}
