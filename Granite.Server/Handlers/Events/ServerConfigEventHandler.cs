using System;
using System.Threading.Tasks;
using GraniteServer.Data;
using GraniteServer.Messaging.Events;
using Microsoft.EntityFrameworkCore;

namespace GraniteServer.Messaging.Handlers.Events;

public class ServerConfigEventHandler : IEventHandler<ServerConfigSyncedEvent>
{
    private readonly GraniteDataContext _dataContext;

    public ServerConfigEventHandler(GraniteDataContext dataContext)
    {
        _dataContext = dataContext;
    }

    async Task IEventHandler<ServerConfigSyncedEvent>.Handle(ServerConfigSyncedEvent @event)
    {
        var config = @event.Data.Config;
        var serverId = @event.OriginServerId;

        var serverEntity = await _dataContext.Servers.FirstOrDefaultAsync(s => s.Id == serverId);

        if (serverEntity != null)
        {
            // Update server name if it has changed
            if (!string.IsNullOrEmpty(config.ServerName))
            {
                serverEntity.Name = config.ServerName;
            }

            // Update all configuration fields
            if (config.Port.HasValue)
                serverEntity.Port = config.Port;
            
            if (config.WelcomeMessage != null)
                serverEntity.WelcomeMessage = config.WelcomeMessage;
            
            if (config.MaxClients.HasValue)
                serverEntity.MaxClients = config.MaxClients;
            
            if (config.Password != null)
                serverEntity.Password = config.Password;
            
            if (config.MaxChunkRadius.HasValue)
                serverEntity.MaxChunkRadius = config.MaxChunkRadius;
            
            if (config.WhitelistMode != null)
                serverEntity.WhitelistMode = config.WhitelistMode;
            
            if (config.AllowPvP.HasValue)
                serverEntity.AllowPvP = config.AllowPvP;
            
            if (config.AllowFireSpread.HasValue)
                serverEntity.AllowFireSpread = config.AllowFireSpread;
            
            if (config.AllowFallingBlocks.HasValue)
                serverEntity.AllowFallingBlocks = config.AllowFallingBlocks;

            _dataContext.Servers.Update(serverEntity);
            await _dataContext.SaveChangesAsync();
        }
    }
}
