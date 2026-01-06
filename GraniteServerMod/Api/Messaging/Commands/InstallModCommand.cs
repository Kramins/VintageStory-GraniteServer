using System;
using GraniteServer.Api.Messaging.Contracts;

namespace GraniteServer.Api.Messaging.Commands;

public class InstallModCommand : MessageBusMessage<InstallModCommandData>
{
    public static string MessageType => typeof(InstallModCommand).Name;
}
