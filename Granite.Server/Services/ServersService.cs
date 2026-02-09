using System;
using System.Linq;
using System.Security.Cryptography;
using Granite.Common.Dto;
using GraniteServer.Data;
using GraniteServer.Data.Entities;
using GraniteServer.Messaging.Commands;
using GraniteServer.Services;
using Microsoft.EntityFrameworkCore;

namespace Granite.Server.Services;

public class ServersService
{
    private readonly ILogger<ServersService> _logger;
    private readonly GraniteDataContext _dbContext;
    private readonly PersistentMessageBusService _messageBus;

    public ServersService(
        ILogger<ServersService> logger,
        GraniteDataContext dbContext,
        PersistentMessageBusService messageBus
    )
    {
        _logger = logger;
        _dbContext = dbContext;
        _messageBus = messageBus;
    }

    public virtual async Task<List<ServerDTO>> GetServersAsync()
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

    public virtual IQueryable<ServerDetailsDTO> GetServerDetailsAsync()
    {
        return _dbContext
            .Servers.Include(s => s.ServerMetrics)
            .OrderBy(s => s.CreatedAt)
            .Select(s => new ServerDetailsDTO
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                CreatedAt = s.CreatedAt,
                IsOnline = s.IsOnline,
                LastSeenAt = s.LastSeenAt,
                CurrentPlayers = _dbContext
                    .PlayerSessions.Count(ps => ps.ServerId == s.Id && ps.LeaveDate == null),
                MaxPlayers = s.MaxClients ?? 0,
                UpTime = s
                    .ServerMetrics.OrderByDescending(m => m.RecordedAt)
                    .Select(m => m.UpTimeSeconds)
                    .FirstOrDefault(),
                MemoryUsageBytes = s
                        .ServerMetrics.OrderByDescending(m => m.RecordedAt)
                        .Select(m => (long)(m.MemoryUsageMB * 1024 * 1024))
                        .FirstOrDefault(),
                ServerIp = string.Empty,
                WorldAgeDays = 0,
                GameVersion = string.Empty,
                WorldName = string.Empty,
                WorldSeed = 0,
            });
    }

    public virtual async Task<ServerDetailsDTO?> GetServerDetailsAsync(Guid serverId)
    {
        return await GetServerDetailsAsync().FirstOrDefaultAsync(s => s.Id == serverId);
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

    public virtual async Task<ServerCreatedResponseDTO> CreateServerAsync(CreateServerRequestDTO request)
    {
        _logger.LogInformation("Creating new server with name: {ServerName}", request.Name);

        // Check if server name already exists
        var nameExists = await _dbContext.Servers.AnyAsync(s => s.Name == request.Name);
        if (nameExists)
        {
            _logger.LogWarning(
                "Server creation failed: Server name '{ServerName}' already exists",
                request.Name
            );
            throw new InvalidOperationException(
                $"A server with the name '{request.Name}' already exists."
            );
        }

        // Generate a cryptographically secure access token
        var accessToken = GenerateSecureToken();

        var serverEntity = new ServerEntity
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            AccessToken = accessToken,
            CreatedAt = DateTime.UtcNow,
            IsOnline = false,
            LastSeenAt = null,

            // Initialize with default config values (can be customized before first startup)
            Port = request.Port ?? 42420,
            WelcomeMessage = request.WelcomeMessage ?? "Welcome to the server!",
            MaxClients = request.MaxClients ?? 16,
            Password = request.Password ?? string.Empty,
            MaxChunkRadius = request.MaxChunkRadius ?? 8,
            WhitelistMode = request.WhitelistMode?.ToString() ?? "None",
            AllowPvP = request.AllowPvP ?? true,
            AllowFireSpread = request.AllowFireSpread ?? true,
            AllowFallingBlocks = request.AllowFallingBlocks ?? true,
        };

        _dbContext.Servers.Add(serverEntity);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Server created successfully with ID: {ServerId}", serverEntity.Id);

        return new ServerCreatedResponseDTO
        {
            Id = serverEntity.Id,
            Name = serverEntity.Name,
            Description = serverEntity.Description,
            CreatedAt = serverEntity.CreatedAt,
            IsOnline = serverEntity.IsOnline,
            LastSeenAt = serverEntity.LastSeenAt,
            AccessToken = accessToken,
        };
    }

    public virtual async Task<ServerDTO?> GetServerByIdAsync(Guid serverId)
    {
        _logger.LogDebug("Fetching server by ID: {ServerId}", serverId);

        var server = await _dbContext.Servers.FindAsync(serverId);
        if (server == null)
        {
            _logger.LogWarning("Server not found: {ServerId}", serverId);
            return null;
        }

        return new ServerDTO
        {
            Id = server.Id,
            Name = server.Name,
            Description = server.Description,
            CreatedAt = server.CreatedAt,
            IsOnline = server.IsOnline,
            LastSeenAt = server.LastSeenAt,
        };
    }

    public virtual async Task<ServerDTO?> UpdateServerAsync(Guid serverId, UpdateServerRequestDTO request)
    {
        _logger.LogInformation("Updating server: {ServerId}", serverId);

        var server = await _dbContext.Servers.FindAsync(serverId);
        if (server == null)
        {
            _logger.LogWarning("Server not found for update: {ServerId}", serverId);
            return null;
        }

        // Check if new name conflicts with another server
        var nameExists = await _dbContext.Servers.AnyAsync(s =>
            s.Name == request.Name && s.Id != serverId
        );
        if (nameExists)
        {
            _logger.LogWarning(
                "Server update failed: Server name '{ServerName}' already exists",
                request.Name
            );
            throw new InvalidOperationException(
                $"A server with the name '{request.Name}' already exists."
            );
        }

        // Update fields
        server.Name = request.Name;
        server.Description = request.Description;
        server.Port = request.Port;
        server.WelcomeMessage = request.WelcomeMessage;
        server.MaxClients = request.MaxClients;
        server.Password = request.Password;
        server.MaxChunkRadius = request.MaxChunkRadius;
        server.WhitelistMode = request.WhitelistMode?.ToString();
        server.AllowPvP = request.AllowPvP;
        server.AllowFireSpread = request.AllowFireSpread;
        server.AllowFallingBlocks = request.AllowFallingBlocks;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Server updated successfully: {ServerId}", serverId);

        return new ServerDTO
        {
            Id = server.Id,
            Name = server.Name,
            Description = server.Description,
            CreatedAt = server.CreatedAt,
            IsOnline = server.IsOnline,
            LastSeenAt = server.LastSeenAt,
        };
    }

    public virtual async Task<bool> DeleteServerAsync(Guid serverId)
    {
        _logger.LogInformation("Deleting server: {ServerId}", serverId);

        var server = await _dbContext.Servers.FindAsync(serverId);
        if (server == null)
        {
            _logger.LogWarning("Server not found for deletion: {ServerId}", serverId);
            return false;
        }

        _dbContext.Servers.Remove(server);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Server deleted successfully: {ServerId}", serverId);
        return true;
    }

    public virtual async Task<TokenRegeneratedResponseDTO?> RegenerateAccessTokenAsync(Guid serverId)
    {
        _logger.LogInformation("Regenerating access token for server: {ServerId}", serverId);

        var server = await _dbContext.Servers.FindAsync(serverId);
        if (server == null)
        {
            _logger.LogWarning("Server not found for token regeneration: {ServerId}", serverId);
            return null;
        }

        var newToken = GenerateSecureToken();
        server.AccessToken = newToken;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Access token regenerated successfully for server: {ServerId}",
            serverId
        );

        return new TokenRegeneratedResponseDTO { Id = server.Id, AccessToken = newToken };
    }

    private static string GenerateSecureToken()
    {
        // Generate a 32-byte (256-bit) cryptographically secure random token
        var tokenBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes);
    }
}
