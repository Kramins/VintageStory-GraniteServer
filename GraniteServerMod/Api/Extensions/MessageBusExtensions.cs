using System;
using System.Reactive.Linq;
using GraniteServer.Api.Messaging;
using GraniteServer.Api.Services;

namespace GraniteServer.Api.Extensions
{
    public static class MessageBusExtensions
    {
        /// <summary>
        /// Gets the message type name for a MessageBusMessage or derived type.
        /// </summary>
        public static string GetMessageType(this MessageBusMessage message)
        {
            return message.GetType().Name;
        }

        public static IDisposable Subscribe<TMessage>(
            this MessageBusService messageBus,
            Action<TMessage> handler
        )
            where TMessage : MessageBusMessage
        {
            return messageBus
                .Subscribe()
                .Where(e => e is TMessage)
                .Select(e => (TMessage)e)
                .Subscribe(handler);
        }
    }
}
