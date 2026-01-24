using System;

namespace GraniteServer.Messaging.Commands;

public class QueryPlayerInventoryCommand : CommandMessage<QueryPlayerInventoryCommandData> { }

public class QueryPlayerInventoryCommandData
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
}
