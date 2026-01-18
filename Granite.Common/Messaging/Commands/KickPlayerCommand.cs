using GraniteServer.Api.Messaging.Contracts;

namespace GraniteServer.Messaging.Commands;

public class KickPlayerCommand : CommandMessage<KickPlayerCommandData>;

public class KickPlayerCommandData
{
    public string PlayerId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
