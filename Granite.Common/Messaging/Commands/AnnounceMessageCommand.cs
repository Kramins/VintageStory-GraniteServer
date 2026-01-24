using GraniteServer.Api.Messaging.Contracts;

namespace GraniteServer.Messaging.Commands;

public class AnnounceMessageCommand : CommandMessage<AnnounceMessageCommandData>;

public class AnnounceMessageCommandData
{
    public string Message { get; set; } = string.Empty;
}
