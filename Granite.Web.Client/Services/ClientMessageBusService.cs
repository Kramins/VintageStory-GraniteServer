using GraniteServer.Messaging;
using GraniteServer.Services;

namespace Granite.Web.Client.Services;

/// <summary>
/// Client-side message bus service that wraps the common MessageBusService
/// for publishing and subscribing to events in the Blazor WebAssembly client.
/// </summary>
public class ClientMessageBusService : MessageBusService
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

        _logger.LogDebug("Publishing message to client message bus: {MessageType}", message.MessageType);
        base.Publish(message);
    }
}
