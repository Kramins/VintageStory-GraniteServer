using System;
using System.Threading.Tasks;
using Granite.Common.Dto;
using GraniteServer.Data;
using GraniteServer.Messaging.Commands;
using GraniteServer.Services;
using Microsoft.EntityFrameworkCore;

namespace Granite.Server.Services;

public class ServerConfigService
{
    private readonly PersistentMessageBusService _messageBus;
    private readonly GraniteDataContext _dbContext;

    public ServerConfigService(PersistentMessageBusService messageBus, GraniteDataContext dbContext)
    {
        _messageBus = messageBus;
        _dbContext = dbContext;
    }

    public virtual async Task<ServerConfigDTO?> GetServerConfigAsync(Guid serverId)
    {
        var server = await _dbContext.Servers.FirstOrDefaultAsync(s => s.Id == serverId);

        if (server == null)
        {
            return null;
        }

        // Return full config from database
        return new ServerConfigDTO
        {
            ServerName = server.Name,
            Port = server.Port,
            WelcomeMessage = server.WelcomeMessage,
            MaxClients = server.MaxClients,
            Password = server.Password,
            MaxChunkRadius = server.MaxChunkRadius,
            WhitelistMode = server.WhitelistMode,
            AllowPvP = server.AllowPvP,
            AllowFireSpread = server.AllowFireSpread,
            AllowFallingBlocks = server.AllowFallingBlocks,
        };
    }

    public virtual async Task SyncServerConfigAsync(Guid serverId)
    {
        var syncCommand = _messageBus.CreateCommand<SyncServerConfigCommand>(
            serverId,
            cmd =>
            {
                // Empty command data - just triggers sync
            }
        );

        await _messageBus.PublishCommandAsync(syncCommand);
    }

    public virtual async Task UpdateServerConfigAsync(Guid serverId, ServerConfigDTO config)
    {
        var updateCommand = _messageBus.CreateCommand<UpdateServerConfigCommand>(
            serverId,
            cmd =>
            {
                cmd.Data.Config = config;
            }
        );

        await _messageBus.PublishCommandAsync(updateCommand);
    }
}
