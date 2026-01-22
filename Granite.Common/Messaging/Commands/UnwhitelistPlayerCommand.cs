using GraniteServer.Api.Messaging.Contracts;

namespace GraniteServer.Messaging.Commands;

public class UnwhitelistPlayerCommand : CommandMessage<UnwhitelistPlayerCommandData>;

public class UnwhitelistPlayerCommandData
{
    public string PlayerId { get; set; } = string.Empty;
}
