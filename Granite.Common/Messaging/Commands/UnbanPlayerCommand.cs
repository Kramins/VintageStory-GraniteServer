using System;

namespace GraniteServer.Messaging.Commands;

public class UnbanPlayerCommand : CommandMessage<UnbanPlayerCommandData> { }

public class UnbanPlayerCommandData
{
    public string PlayerId { get; set; } = string.Empty;
}
