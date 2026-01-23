using GraniteServer.Api.Messaging.Contracts;

namespace GraniteServer.Messaging.Commands;

public class SyncServerConfigCommand : CommandMessage<SyncServerConfigCommandData>;

public class SyncServerConfigCommandData
{
    // Empty payload - command just triggers a sync
}
