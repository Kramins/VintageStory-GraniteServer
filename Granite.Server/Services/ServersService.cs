using System;
using Granite.Common.Dto;
using GraniteServer.Data;
using Microsoft.EntityFrameworkCore;

namespace Granite.Server.Services;

public class ServersService
{
    private readonly GraniteDataContext _dbContext;

    public ServersService(GraniteDataContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<ServerDTO>> GetServersAsync()
    {
        var servers = await _dbContext.Servers
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();

        return servers.Select(s => new ServerDTO
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            CreatedAt = s.CreatedAt
        }).ToList();
    }
}
