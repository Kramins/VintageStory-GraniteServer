using Granite.Common.Dto;
using GraniteServer.Api.Messaging.Contracts;

namespace GraniteServer.Messaging.Commands;

public class UpdateServerConfigCommand : CommandMessage<UpdateServerConfigCommandData>;

public class UpdateServerConfigCommandData
{
    public ServerConfigDTO Config { get; set; } = new();
}
