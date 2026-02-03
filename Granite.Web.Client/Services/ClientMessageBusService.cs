using System.Reactive.Linq;
using GraniteServer.Messaging;
using GraniteServer.Services;

namespace Granite.Web.Client.Services;

/// <summary>
/// Client-side message bus service that wraps the common MessageBusService
/// for publishing and subscribing to events in the Blazor WebAssembly client.
/// </summary>
public class ClientMessageBusService : MessageBusService // Consider not inheriting and just fully implementing to avoid confusion
{
    private readonly ILogger<ClientMessageBusService> _logger;

    public ClientMessageBusService(ILogger<ClientMessageBusService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Publishes an event to the message bus with client-side logging.
    /// </summary>
    public new void Publish(MessageBusMessage message)
    {
        if (message == null)
        {
            _logger.LogWarning("Attempted to publish null message to client message bus");
            return;
        }

        _logger.LogDebug(
            "Publishing message to client message bus: {MessageType}",
            message.MessageType
        );
        base.Publish(message);
    }

    public IObservable<T> GetObservable<T>(Guid serverId)
        where T : MessageBusMessage
    {
        // Reason for doing this here, if we have the serverId and event T we can track what active filters we have and inform the server side
        // to only send us those events, reducing bandwidth and processing on the client side.
        // For now we will get all events and filter here.
        return this.GetObservable()
            .Where(msg => msg is T)
            .Where(msg => msg.OriginServerId == serverId)
            .Select(msg => (T)msg);
    }
}
