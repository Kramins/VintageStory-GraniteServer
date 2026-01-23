using System;
using Granite.Common.Dto;
using GraniteServer.Data;
using Microsoft.EntityFrameworkCore;

namespace Granite.Server.Services;

public class ServersService
{
    private readonly ILogger<ServersService> _logger;
    private readonly GraniteDataContext _dbContext;

    public ServersService(ILogger<ServersService> logger, GraniteDataContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<List<ServerDTO>> GetServersAsync()
    {
        var servers = await _dbContext.Servers.OrderBy(s => s.CreatedAt).ToListAsync();

        return servers
            .Select(s => new ServerDTO
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                CreatedAt = s.CreatedAt,
                IsOnline = s.IsOnline,
                LastSeenAt = s.LastSeenAt,
            })
            .ToList();
    }

    internal async Task MarkServerOfflineAsync(Guid serverId)
    {
        var server = await _dbContext.Servers.FindAsync(serverId);
        if (server != null)
        {
            server.IsOnline = false;
            server.LastSeenAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }
    }

    internal async Task MarkServerOnlineAsync(Guid serverId)
    {
        var server = await _dbContext.Servers.FindAsync(serverId);
        if (server != null)
        {
            server.IsOnline = true;
            server.LastSeenAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }
    }
}
