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
    private readonly ILogger<ServerConfigService> _logger;
    private readonly PersistentMessageBusService _messageBus;
    private readonly GraniteDataContext _dbContext;

    public ServerConfigService(ILogger<ServerConfigService> logger, PersistentMessageBusService messageBus, GraniteDataContext dbContext)
    {
        _logger = logger;
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
            AccessToken = server.AccessToken,
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
        // Update database first - control plane is the source of truth
        var server = await _dbContext.Servers.FirstOrDefaultAsync(s => s.Id == serverId);
        if (server == null)
        {
            throw new InvalidOperationException($"Server with ID {serverId} not found");
        }

        // Update all config properties in the database (except AccessToken which is managed separately)
        server.Name = config.ServerName ?? server.Name;
        server.Port = config.Port;
        server.WelcomeMessage = config.WelcomeMessage;
        server.MaxClients = config.MaxClients;
        server.Password = config.Password;
        server.MaxChunkRadius = config.MaxChunkRadius;
        server.WhitelistMode = config.WhitelistMode;
        server.AllowPvP = config.AllowPvP;
        server.AllowFireSpread = config.AllowFireSpread;
        server.AllowFallingBlocks = config.AllowFallingBlocks;
        // Note: AccessToken is NOT updated here - it's managed via RegenerateAccessToken endpoint

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Server configuration updated in database for server {ServerId}", serverId);

        // NOTE: Potential race condition if a game admin modifies config via in-game commands
        // simultaneously. The ServerConfigSyncedEvent handler will overwrite with game server values.
        // Consider implementing optimistic locking or timestamp-based conflict detection if this becomes an issue.

        // Push configuration to the game server
        var updateCommand = _messageBus.CreateCommand<UpdateServerConfigCommand>(
            serverId,
            cmd =>
            {
                cmd.Data.Config = config;
            }
        );

        await _messageBus.PublishCommandAsync(updateCommand);
        _logger.LogInformation("UpdateServerConfigCommand sent to game server {ServerId}", serverId);
    }
}
