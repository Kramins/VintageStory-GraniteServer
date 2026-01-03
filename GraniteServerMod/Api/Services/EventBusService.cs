using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using GraniteServer.Api.Models;
using Vintagestory.API.Common;

namespace GraniteServer.Api.Services
{
    /// <summary>
    /// Centralized event bus service that allows any service to publish events
    /// and clients to subscribe to events via SSE or other mechanisms.
    ///
    /// Thread-safe singleton using System.Threading.Channels for backpressure and multiple subscribers.
    /// </summary>
    public class EventBusService
    {
        private readonly ILogger _logger;
        private readonly Channel<EventDto> _channel;

        /// <summary>
        /// Capacity of the event channel. If exceeded, oldest events may be dropped (BoundedChannelFullMode.DropOldest).
        /// </summary>
        private const int ChannelCapacity = 1000;

        public EventBusService(ILogger logger)
        {
            _logger = logger;
            var channelOptions = new BoundedChannelOptions(ChannelCapacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
            };
            _channel = Channel.CreateBounded<EventDto>(channelOptions);
        }

        /// <summary>
        /// Publishes an event to the bus. This is thread-safe and non-blocking.
        /// If the channel is at capacity, the oldest event will be dropped.
        /// </summary>
        public void Publish(EventDto @event)
        {
            if (@event == null)
            {
                _logger.Warning("[EventBus] Attempted to publish null event");
                return;
            }

            try
            {
                if (!_channel.Writer.TryWrite(@event))
                {
                    _logger.Warning(
                        $"[EventBus] Failed to publish event {@event.EventType} (channel may be closed)"
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"[EventBus] Error publishing event: {ex.Message}");
            }
        }

        /// <summary>
        /// Returns a ChannelReader that can be used by subscribers to read events asynchronously.
        /// Each subscriber gets their own reader, so they can subscribe independently.
        /// </summary>
        public ChannelReader<EventDto> Subscribe()
        {
            return _channel.Reader;
        }

        /// <summary>
        /// Graceful shutdown: completes the channel writer so no new events can be published.
        /// Existing readers will continue to read until all queued events are consumed.
        /// </summary>
        public void Shutdown()
        {
            try
            {
                _channel.Writer.Complete();
                _logger.Notification("[EventBus] EventBus shutdown complete");
            }
            catch (Exception ex)
            {
                _logger.Error($"[EventBus] Error during shutdown: {ex.Message}");
            }
        }
    }
}
