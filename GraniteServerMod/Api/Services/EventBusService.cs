using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using GraniteServer.Api.Models;
using GraniteServer.Api.Models.Events;
using Vintagestory.API.Common;

namespace GraniteServer.Api.Services
{
    /// <summary>
    /// Centralized event bus service that allows any service to publish events
    /// and clients to subscribe to events via SSE or other mechanisms.
    ///
    /// Thread-safe singleton using Rx.NET synchronized ReplaySubject for broadcast to all subscribers.
    /// </summary>
    public class EventBusService
    {
        private readonly ILogger _logger;
        private readonly GraniteServerConfig _config;
        private readonly ISubject<EventDto> _subject;

        /// <summary>
        /// Capacity of the replay buffer. Stores up to this many events for new subscribers to catch up.
        /// </summary>
        private const int ReplayBufferSize = 1000;

        public EventBusService(ILogger logger, GraniteServerConfig config)
        {
            _logger = logger;
            _config = config;
            _subject = Subject.Synchronize(new ReplaySubject<EventDto>(ReplayBufferSize));
        }

        /// <summary>
        /// Publishes an event to the bus. This is thread-safe and non-blocking.
        /// All current subscribers will receive the event immediately.
        /// </summary>
        public void Publish(EventDto @event)
        {
            if (@event == null)
            {
                _logger.Warning("[EventBus] Attempted to publish null event");
                return;
            }
            if (@event.ServerId == Guid.Empty)
            {
                @event.ServerId = _config.ServerId;
            }
            try
            {
                _subject.OnNext(@event);
            }
            catch (Exception ex)
            {
                _logger.Error($"[EventBus] Error publishing event: {ex.Message}");
            }
        }

        /// <summary>
        /// Returns an IObservable that can be subscribed to receive events.
        /// Each subscriber will receive all events from the ReplaySubject (broadcast).
        /// New subscribers also receive buffered past events up to ReplayBufferSize.
        /// </summary>
        public IObservable<EventDto> Subscribe()
        {
            return _subject.AsObservable();
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
                _logger.Notification("[EventBus] EventBus shutdown complete");
            }
            catch (Exception ex)
            {
                _logger.Error($"[EventBus] Error during shutdown: {ex.Message}");
            }
        }
    }
}
