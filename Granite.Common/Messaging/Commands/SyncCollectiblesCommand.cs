namespace GraniteServer.Messaging.Commands;

public class SyncCollectiblesCommand : CommandMessage<SyncCollectiblesCommandData> { }

public class SyncCollectiblesCommandData
{
    // No data needed - this is a trigger to sync all collectibles from the mod
}
