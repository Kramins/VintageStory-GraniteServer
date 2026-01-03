using System;

namespace GraniteServer.Api.Models
{
    /// <summary>
    /// Represents an event that can be published through the EventBus and streamed to clients via SSE.
    /// </summary>
    public class EventDto
    {
        /// <summary>
        /// Unique identifier for this event (used for SSE Last-Event-ID).
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Event type/name (e.g., "player.joined", "world.saved", "collectible.spawned").
        /// </summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// Event data as JSON string or serialized object.
        /// </summary>
        public object? Data { get; set; }

        /// <summary>
        /// Timestamp when the event was created.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional source identifier (e.g., "world", "server", "player-session").
        /// </summary>
        public string? Source { get; set; }
    }
}
