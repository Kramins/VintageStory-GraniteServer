using GraniteServer.Api.Messaging.Contracts;

namespace GraniteServer.Messaging.Commands;

public class WhitelistPlayerCommand : CommandMessage<WhitelistPlayerCommandData>;

public class WhitelistPlayerCommandData
{
    public string PlayerId { get; set; } = string.Empty;
    public string? Reason { get; set; }
}
