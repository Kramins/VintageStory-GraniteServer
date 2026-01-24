using System;
using System.Linq;
using System.Threading.Tasks;
using Granite.Common.Dto;
using GraniteServer.Data;
using GraniteServer.Messaging.Commands;
using GraniteServer.Services;
using Microsoft.EntityFrameworkCore;

namespace Granite.Server.Services;

public class ServerService
{
    private readonly ILogger<ServerService> _logger;
    private readonly GraniteDataContext _dataContext;
    private readonly PersistentMessageBusService _messageBus;

    public ServerService(
        ILogger<ServerService> logger,
        GraniteDataContext dataContext,
        PersistentMessageBusService messageBus
    )
    {
        _logger = logger;
        _dataContext = dataContext;
        _messageBus = messageBus;
    }

    public async Task<ServerStatusDTO?> GetServerStatusAsync(Guid serverId)
    {
        var server = await _dataContext
            .Servers.Include(s => s.ServerMetrics)
            .Include(s => s.Players)
            .FirstOrDefaultAsync(s => s.Id == serverId);

        if (server == null)
        {
            return null;
        }

        // Get the latest metrics
        var latestMetrics = server
            .ServerMetrics.OrderByDescending(m => m.RecordedAt)
            .FirstOrDefault();

        // Get current online players
        var currentPlayers = await _dataContext
            .PlayerSessions.Where(ps => ps.ServerId == serverId && ps.LeaveDate == null)
            .CountAsync();

        // Get uptime from latest metrics
        var upTimeSeconds = latestMetrics?.UpTimeSeconds ?? 0;

        return new ServerStatusDTO
        {
            ServerIp = string.Empty, // Not tracked in current schema
            UpTime = upTimeSeconds,
            CurrentPlayers = currentPlayers,
            MaxPlayers = server.MaxClients ?? 0,
            ServerName = server.Name,
            WorldAgeDays = 0, // Not tracked in current schema
            MemoryUsageBytes =
                latestMetrics != null ? (long)(latestMetrics.MemoryUsageMB * 1024 * 1024) : 0,
            IsOnline = server.IsOnline,
            GameVersion = string.Empty, // Not tracked in current schema
            WorldName = string.Empty, // Not tracked in current schema
            WorldSeed = 0, // Not tracked in current schema
        };
    }

    public async Task AnnounceMessageAsync(Guid serverId, string message)
    {
        var command = _messageBus.CreateCommand<AnnounceMessageCommand>(
            serverId,
            cmd =>
            {
                cmd.Data.Message = message;
            }
        );

        await _messageBus.PublishCommandAsync(command);

        _logger.LogInformation(
            "Announced message to server {ServerId}: {Message}",
            serverId,
            message
        );
    }
}
