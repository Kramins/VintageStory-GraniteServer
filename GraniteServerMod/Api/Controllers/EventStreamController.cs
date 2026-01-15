using System;
using System.Text.Json;
using System.Threading.Tasks;
using GenHTTP.Api.Content;
using GenHTTP.Api.Infrastructure;
using GenHTTP.Modules.ServerSentEvents;
using GraniteServer.Api.Models;
using GraniteServer.Api.Services;
using Vintagestory.API.Common;

namespace GraniteServer.Api.Controllers
{
    /// <summary>
    /// Factory for creating an EventSource that streams real-time events via SSE.
    ///
    /// Integrates with EventBusService to push server/game events to connected clients
    /// over HTTP Server-Sent Events (SSE).
    ///
    /// Endpoint: GET /api/events
    /// Auth: Requires Authorization: Bearer {token} header (enforced by bearer auth concern in GenHttpHostedService)
    /// Returns: text/event-stream with JSON event data
    /// </summary> </summary>
    public class EventStreamHandler
    {
        private readonly MessageBusService _messageBus;
        private readonly ILogger _logger;

        public EventStreamHandler(MessageBusService messageBus, ILogger logger)
        {
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Generator function that streams events from the EventBus to the connected client.
        /// Called by GenHTTP's EventSource for each new connection.
        ///
        /// The generator subscribes to the EventBus and sends each event as an SSE frame
        /// with id, event type, and JSON data until the client disconnects.
        /// </summary>
        public async ValueTask StreamEventsAsync(IEventConnection connection)
        {
            var observable = _messageBus.GetObservable();
            var subscription = default(IDisposable);

            try
            {
                _logger.Notification("[EventStream] New SSE client connected");

                var tcs = new TaskCompletionSource<bool>();

                subscription = observable.Subscribe(
                    onNext: async (@event) =>
                    {
                        try
                        {
                            var jsonString = JsonSerializer.Serialize(@event);
                            var success = await connection.DataAsync(jsonString);

                            // DataAsync returns false when send fails (client disconnected)
                            if (!success)
                            {
                                _logger.Notification(
                                    "[EventStream] SSE client disconnected (send failed)"
                                );
                                tcs.TrySetResult(false);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Warning($"[EventStream] Error sending event: {ex.Message}");
                            tcs.TrySetException(ex);
                        }
                    },
                    onError: (error) =>
                    {
                        _logger.Error($"[EventStream] Observable error: {error.Message}");
                        tcs.TrySetException(error);
                    },
                    onCompleted: () =>
                    {
                        _logger.Notification("[EventStream] Observable completed");
                        tcs.TrySetResult(true);
                    }
                );

                // Wait until subscription completes or client disconnects
                await tcs.Task;
            }
            catch (OperationCanceledException)
            {
                _logger.Notification("[EventStream] SSE stream cancelled");
            }
            catch (Exception ex)
            {
                _logger.Error($"[EventStream] Error during event streaming: {ex.Message}");
                try
                {
                    await connection.DataAsync(ex.Message, eventType: "error");
                    await connection.RetryAsync(10);
                }
                catch
                {
                    // If error reporting fails, just exit
                }
            }
            finally
            {
                subscription?.Dispose();
            }
        }
    }
}
