using System;
using GraniteServer.Messaging.Events;

namespace Granite.Server.Extensions;

public static class LoggerExtensions
{

    public static void LogInfo(this ILogger logger, string message, EventMessage eventMessage)
    {
        logger.LogInformation(
            "[{ServerId}] {EventType}: {Message}",
            message,
            eventMessage.OriginServerId,
            eventMessage.MessageType
        );
    }
}
